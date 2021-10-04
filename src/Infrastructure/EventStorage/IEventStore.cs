using Infrastructure.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.EventStorage
{
    public interface IEventStore
    {
        Task AppendToStreamAsync(string streamName, IEnumerable<IEvent> events);

        /// <throws><see cref="OptimisticConcurrencyException"/></throws>
        Task AppendToStreamAsync(string streamName, long expectedVersion, IEnumerable<IEvent> events);

        Task<EventStreamSlice> ReadStreamForwardAsync(string streamName, long from, int count);

        Task<CategoryStreamsSlice> ReadStreamsFromCategoryAsync(string category, long from, int count);

        Task<CategoryStreamsSlice> ReadStreamsFromCategoryAsync(string category, long from, int count, long maxEventNumber);

        Task<string> ReadLastStreamFromCategory(string category, int offset = 0);

        Task<bool> CheckStreamExistenceAsync(string streamName);
    }
}
 