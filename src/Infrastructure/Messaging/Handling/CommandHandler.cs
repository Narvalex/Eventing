using Infrastructure.EventSourcing;
using Infrastructure.EventSourcing.Transactions;
using Infrastructure.Utils;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    /// <summary>
    /// A comand handler base class.
    /// </summary>
    public abstract class CommandHandler<T> : MessageHandler<T>, ICommandHandler where T : class, IEventSourced
    {
        private readonly ISagaResultAwaiter awaiter;

        public CommandHandler(IEventSourcedRepository repo)
            : base(repo)
        { }

        public CommandHandler(IEventSourcedRepository repo, ISagaResultAwaiter awaiter)
            : this(repo)
        {
            this.awaiter = Ensured.NotNull(awaiter, nameof(awaiter));
        }

        protected Task<IOnlineTransaction> BeginTransactionAsync(ICommandInTransit command) => this.repo.NewTransaction(command);

        protected async Task<IHandlingResult> Await(string streamId, Func<T, IHandlingResult?> awaitLogic)
        {
            return await this.awaiter.Await(streamId, awaitLogic);
        }

        protected async Task<IHandlingResult> Await(string streamId, Func<T, Task<IHandlingResult?>> awaitLogic)
        {
            return await this.awaiter.Await(streamId, awaitLogic);
        }

        protected HandlingResult Ok()
        {
            return new HandlingResult(true);
        }

        protected Response<TPayload> Ok<TPayload>(TPayload dto)
        {
            return new Response<TPayload>(dto, true);
        }

        protected dynamic Reject(params string[] messages)
        {
            throw new RequestRejectedException(messages);
        }

        protected Response<TPayload> Reject<TPayload>(TPayload dto, params string[] messages)
        {
            return new Response<TPayload>(dto, false, messages);
        }
    }
}
