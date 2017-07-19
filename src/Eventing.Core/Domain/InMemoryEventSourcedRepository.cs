using System;
using System.Threading.Tasks;

namespace Eventing.Core.Domain
{
    public class InMemoryEventSourcedRepository : IEventSourcedRepository
    {
        public Task SaveAsync(IEventSourced eventSourced)
        {
            throw new NotImplementedException();
        }

        Task<T> IEventSourcedReader.GetAsync<T>(string streamName)
        {
            throw new NotImplementedException();
        }
    }
}
