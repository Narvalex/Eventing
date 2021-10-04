using Infrastructure.EventSourcing;
using Infrastructure.EventStorage;
using Infrastructure.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public abstract class MessageHandler<T> : RequestHandler where T : class, IEventSourced
    {
        internal readonly IEventSourcedRepository repo;

        public MessageHandler(IEventSourcedRepository repo)
           : base(repo)
        {
            this.repo = Ensured.NotNull(repo, nameof(repo));
        }

        protected async Task CommitAsync(T eventSourced)
        {
            await this.repo.CommitAsync(eventSourced);
        }

        public async Task UnsafeCommit<TOtherEventSouced>(TOtherEventSouced otherEventSourced) where TOtherEventSouced : class, IEventSourced
        {
            await this.repo.CommitAsync(otherEventSourced);
        }

        protected async Task AppendAsync(ICommand cmd, params IEventInTransit[] events)
        {
            await this.repo.AppendAsync<T>(cmd, events);
        }

        /*** This makes no sense. No free idempotency here */
        //protected async Task AppendAsync(IEvent e, params IEventInTransit[] events)
        //{
        //    await this.repo.AppendAsync<T>(e, events);
        //}

        /// <summary>
        /// Generates a empty event sourced instance of <see cref="T"/>.
        /// </summary>
        /// <returns>The event souced instance.</returns>
        protected async Task<T> NewEventSourced(string id)
        {
            var streamName = EventStream.GetStreamName<T>(id);
            var entity = await this.repo.TryGetByStreamNameEvenIfDoesNotExistsAsync<T>(streamName);
            if (entity is null)
                return EventSourcedCreator.New<T>(); // Brand new entity
            if (entity.Metadata.Exists)
                throw new OptimisticConcurrencyException($"The entity {streamName} already exists."); // The entity was created before
            return entity; // Posible resurrection
        }

        /// <summary>
        /// Rehydrates the instance of event sourced type of <see cref="T"/>, owned by 
        /// the command handler.
        /// </summary>
        protected async Task<T?> TryGetByIdAsync(string streamId)
        {
            return await this.repo.TryGetByIdAsync<T>(streamId);
        }

        protected async Task<T> GetOrCreateByIdAsync(string streamId)
        {
            var eventSourced = await this.repo.TryGetByIdAsync<T>(streamId);
            if (eventSourced is null)
                eventSourced = await this.NewEventSourced(streamId);

            return eventSourced;
        }

        protected async Task<T> GetByIdAsync(string streamId)
        {
            return await this.repo.GetByIdAsync<T>(streamId);
        }

        protected async Task<bool> ExistsAsync(string streamId)
        {
            return await this.repo.ExistsAsync<T>(EventStream.GetStreamName<T>(streamId));
        }

        protected IAsyncEnumerable<T> GetAsAsyncStream()
            => this.repo.GetAsAsyncStream<T>();
    }
}
