using System.Threading.Tasks;

namespace Eventing.Core.Domain
{
    public interface IEventSourcedReader
    {
        Task<T> GetAsync<T>(string streamName) where T : class, IEventSourced, new();
    }
}
