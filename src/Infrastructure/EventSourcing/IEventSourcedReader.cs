using Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing
{
    public interface IEventSourcedReader
    {
        /// <summary>
        /// Gets an event sourced entity by replaying its historical events.
        /// </summary>
        /// <typeparam name="T">The event sourced type</typeparam>
        /// <param name="streamName">The stream name of historical events.</param>
        /// <returns>The event sourced entity or its default value. If it is a class it will be null.</returns>
        Task<T?> GetByStreamNameAsync<T>(string streamName) where T : class, IEventSourced;

        Task<T?> GetByStreamNameAsync<T>(string streamName, long maxVersion) where T : class, IEventSourced;

        Task<IEventSourced?> GetByStreamNameAsync(Type type, string streamName);

        /// <summary>
        /// Useful to perform Linq queries.
        /// </summary>
        IAsyncEnumerable<T> GetAsAsyncStream<T>() where T : class, IEventSourced;

        IAsyncEnumerable<T> GetAsAsyncStream<T>(IEvent contextLimitEvent) where T : class, IEventSourced;

        Task<bool> ExistsAsync<T>(string streamName) where T : class, IEventSourced;

        Task<bool> ExistsAsync(Type type, string streamName);

        Task<string> GetLastEventSourcedId<T>(int offset = 0);

        Task<T?> TryGetByStreamNameEvenIfDoesNotExistsAsync<T>(string streamName) where T : class, IEventSourced;

        Task<IEventSourced?> TryGetByStreamNameEvenIfDoesNotExistsAsync(Type type, string streamName);

        Task AwaitUntilTransactionGoesOffline(string transactionId);
    }
}
