using System.Threading.Tasks;

namespace Eventing.Core.Domain
{
    public interface IEventSourcedReader
    {
        // StreamName = StreamCagegory + StreamId
        Task<T> GetByIdAsync<T>(string streamId) where T : class, IEventSourced, new();
        Task<T> GetByNameAsync<T>(string streamName) where T : class, IEventSourced, new();
    }
}
