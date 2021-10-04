using Infrastructure.EventSourcing;
using Infrastructure.EventStorage;
using Infrastructure.Logging;
using Infrastructure.Processing;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public class CommandBus : ICommandBus, ICommandHandlerRegistry
    {
        private readonly ILogLite logger = LogManager.GetLoggerFor<CommandBus>();

        private readonly Dictionary<Type, ICommandHandler> handlersByType = new Dictionary<Type, ICommandHandler>();
        private readonly string commandsNamespace;
        private readonly IExclusiveWriteLock writeLock;
        private readonly IOnCommandRejected rejectionHook;
        private readonly IOnCommandFailed failureHook;

        /// <summary>
        /// Creates a new Command Bus instance.
        /// </summary>
        /// <param name="rejectionHook">The hook to react on command rejection.</param>
        /// <param name="failureHook">The hook to react on command failure.</param>
        public CommandBus(string commandsNamespace, IExclusiveWriteLock writeLock, IOnCommandRejected? rejectionHook = null, IOnCommandFailed? failureHook = null)
        {
            this.commandsNamespace = Ensured.NotEmpty(commandsNamespace, nameof(commandsNamespace));
            this.writeLock = writeLock.EnsuredNotNull(nameof(writeLock));
            this.rejectionHook = rejectionHook ?? new NoopOnCommandRejectedHook();
            this.failureHook = failureHook ?? new NoopOnCommandFailed();
        }

        public ICommandBus Register(ICommandHandler handler)
        {
            var genericHandler = typeof(ICommandHandler<>);
            var genericHandlerWithResponse = typeof(ICommandHandler<,>);
            var supportedCommandTypes = handler.GetType()
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Transform(d => d == genericHandler || d == genericHandlerWithResponse))
                .Select(i => i.GetGenericArguments()[0])
                .ToList();

            if (this.handlersByType.Keys.Any(registeredType => supportedCommandTypes.Contains(registeredType)))
                throw new ArgumentException("The command handled by the received handler has already a registered handler.");

            supportedCommandTypes.ForEach(commandType =>
                this.handlersByType.Add(Ensured.NamespaceIsValid(commandType, this.commandsNamespace), handler));
            return this;
        }

        public async Task<IHandlingResult> Send(ICommandInTransit command, IMessageMetadata metadata)
        {
            if (!this.writeLock.IsAcquired)
            {
                try
                {
                    await this.writeLock.WaitLockAcquisition(TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "Error on waiting write lock");
                    return new HandlingResult(false, "Could not process your request at this moment. Please try again or contact the system administrator.");
                }
            }

            command.SetMetadata(metadata);

            var cmdType = command.GetType();

            if (!this.handlersByType.TryGetValue(cmdType, out var handler))
            {
                var ex = new KeyNotFoundException($"No handler for command of type '{cmdType.Name}' was found.");
                await this.OnFailure(command, cmdType, ex);
                throw ex;
            }

            var validationResult = command.ExecuteBasicValidation();
            if (!validationResult.IsValid)
            {
                await this.OnRejection(handler, command, cmdType, validationResult.Messages);
                return new HandlingResult(false, validationResult.Messages);
            }

            IHandlingResult result;
            try
            {
                try
                {
                    result = await ((dynamic)handler).Handle((dynamic)command);
                }
                catch (OptimisticConcurrencyException)
                {
                    this.logger.Warning("Concurrency exception detected. User: " + metadata.AuthorName + ". Retrying now...");
                    await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(500, 3000)));
                    result = await ((dynamic)handler).Handle((dynamic)command);
                    this.logger.Success("Concurrency exception resolved successfully on retry!");
                }
            }
            catch (ParameterException ex)
            {
                result = new HandlingResult(false, ex.Messages);
            }
            catch (RequestRejectedException ex)
            {
                result = new HandlingResult(false, ex.Messages);
            }
            catch (InvalidEventException ex)
            {
                result = new HandlingResult(false, ex.Messages);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"Unhanled exception in handler {handler.ToString()} when processing command of type {cmdType.Name}");
                result = await this.OnFailure(command, cmdType, ex);
                if (result is null)
                    throw;
                else
                    return result;
            }

            if (!result.Success)
                await this.OnRejection(handler, command, cmdType, result.Messages);

            return result;
        }

        private async Task OnRejection(ICommandHandler handler, ICommandInTransit command, Type cmdType, string[] messages)
        {
            try
            {
                await this.rejectionHook.OnCommandRejected(command, messages);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"Unhanled exception on command rejection hook, with handler {handler.ToString()} when processing command of type {cmdType.Name}");

                throw;
            }
        }

        private async Task<IHandlingResult> OnFailure(ICommand command, Type cmdType, Exception exception)
        {
            try
            {
                return await this.failureHook.OnCommandFailed(command, exception);
            }
            catch (Exception ex)
            {
                var aggregateEx = new AggregateException(exception, ex);
                this.logger.Error(aggregateEx, $"Unhanled exception on command failure hook when processing command of type {cmdType.Name}");

                throw aggregateEx;
            }
        }
    }
}
