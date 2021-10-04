using Infrastructure.Logging;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public class QueryBus : IQueryBus, IQueryHandlerRegistry
    {
        private readonly ILogLite logger = LogManager.GetLoggerFor<QueryBus>();

        private readonly Dictionary<Type, IQueryHandler> handlersByType = new Dictionary<Type, IQueryHandler>();
        private readonly string queriesNamespace;
        private readonly IOnQueryRejected rejectionHook;
        private readonly IOnQueryFailed failureHook;

        public QueryBus(string queriesNamespace, IOnQueryRejected? rejectionHook = null, IOnQueryFailed? failureHook = null)
        {
            this.queriesNamespace = Ensured.NotEmpty(queriesNamespace, nameof(queriesNamespace));
            this.rejectionHook = rejectionHook ?? new NoopOnQueryRejected();
            this.failureHook = failureHook ?? new NoopOnQueryFailed();
        }

        public IQueryBus Register(IQueryHandler handler)
        {
            var genericHandler = typeof(IQueryHandler<>);
            // For new type of query handler
            var queryResponseHandler = typeof(IQueryHandler<,>);

            var supportedQueryTypes = handler.GetType()
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Transform(d => d == genericHandler || d == queryResponseHandler))
                .Select(i => i.GetGenericArguments()[0])
                .ToList();

            if (this.handlersByType.Keys.Any(registeredType => supportedQueryTypes.Contains(registeredType)))
            {
                var confictingQuery = this.handlersByType
                     .Keys
                     .First(registeredType =>
                     supportedQueryTypes.Contains(registeredType));

                throw new ArgumentException(
                    $"The query '{confictingQuery.Name}' handled by the received handler of type '{handler.GetType().Name}' has already a registered handler {handlersByType[confictingQuery].GetType().Name}.");
            }

            supportedQueryTypes.ForEach(commandType => this.handlersByType.Add(Ensured.NamespaceIsValid(commandType, this.queriesNamespace), handler));
            return this;
        }

        public async Task<IHandlingResult> Send<T>(T query, IMessageMetadata metadata) where T : IQueryInTransit
        {
            query.SetMetadata(metadata);

            var queryType = query.GetType();

            if (!this.handlersByType.TryGetValue(queryType, out var handler))
                throw new KeyNotFoundException($"No handler for query of type '{queryType.Name}' was found.");

            var validationResult = query.ExecuteBasicValidation();
            if (!validationResult.IsValid)
            {
                await this.OnRejection(handler, query, queryType, validationResult.Messages);
                return new HandlingResult(false, validationResult.Messages);
            }

            IHandlingResult result;
            try
            {
                result = await ((dynamic)handler).Handle((dynamic)query);
            }
            catch (RequestRejectedException ex)
            {
                result = new HandlingResult(false, ex.Messages);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"Unhanled exception in handler {handler.ToString()} when processing query of type {queryType.Name}");
                result = await this.OnFailure(query, queryType, ex);
                if (result is null)
                    throw;
                else
                    return result;
            }

            if (!result.Success)
                await this.OnRejection(handler, query, queryType, result.Messages);

            return result;
        }

        private async Task OnRejection(IQueryHandler handler, IQueryInTransit query, Type queryType, string[] messages)
        {
            try
            {
                await this.rejectionHook.OnQueryRejected(query, messages);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"Unhanled exception on query rejection hook, with handler {handler.ToString()} when processing query of type {queryType.Name}");

                throw;
            }
        }

        private async Task<IHandlingResult> OnFailure(IQuery query, Type queryType, Exception exception)
        {
            try
            {
                return await this.failureHook.OnQueryFailed(query, exception);
            }
            catch (Exception ex)
            {
                var aggregateEx = new AggregateException(exception, ex);
                this.logger.Error(aggregateEx, $"Unhanled exception on query failure hook when processing query of type {queryType.Name}");
                throw;
            }
        }
    }
}
