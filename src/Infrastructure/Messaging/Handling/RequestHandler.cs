using Infrastructure.EventSourcing;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public abstract class RequestHandler
    {
        private readonly IEventSourcedReader reader;

        public RequestHandler(IEventSourcedReader reader)
        {
            this.reader = reader.EnsuredNotNull(nameof(reader));
        }

        /// <summary>
        /// Rehydrates the specific instance of event sourced type of <see cref="TEventSouced"/>, or returns null.
        /// </summary>
        protected async Task<TEventSourced?> TryGetByIdAsync<TEventSourced>(string streamId) where TEventSourced : class, IEventSourced
        {
            return await this.reader.TryGetByIdAsync<TEventSourced>(streamId);
        }

        protected Task<T?> TryGetByIdAsync<T>(string streamId, IEvent @event) where T : class, IEventSourced
        {
            return this.reader.TryGetByIdAsync<T>(streamId, @event);
        }

        protected async Task<TEventSourced> GetByIdAsync<TEventSourced>(string streamId) where TEventSourced : class, IEventSourced
        {
            return await this.reader.GetByIdAsync<TEventSourced>(streamId);
        }

        protected Task<T> GetByIdAsync<T>(string streamId, IEvent @event) where T : class, IEventSourced
        {
            return this.reader.GetByIdAsync<T>(streamId, @event);
        }

        protected async Task<bool> ExistsAsync<TEventSourced>(string streamId) where TEventSourced : IEventSourced
        {
            return await this.reader.ExistsAsync<TEventSourced>(streamId);
        }

        protected IAsyncEnumerable<TEventSourced> GetAsAsyncStream<TEventSourced>() where TEventSourced : class, IEventSourced
          => this.reader.GetAsAsyncStream<TEventSourced>();

        /// <summary>
        /// Gets <see cref="TEventSourced"/> at the time of the provided event.
        /// </summary>
        /// <typeparam name="TEventSourced">The aggregate or event sourced entity</typeparam>
        /// <param name="maxEventNumberInContext">The event that represents the point in time</param>
        /// <returns>All the <see cref="TEventSourced"/> at the time provided by the event.</returns>
        protected IAsyncEnumerable<TEventSourced> GetAsAsyncStream<TEventSourced>(IEvent maxEventNumberInContext) where TEventSourced : class, IEventSourced
         => this.reader.GetAsAsyncStream<TEventSourced>(maxEventNumberInContext);
    }
}
