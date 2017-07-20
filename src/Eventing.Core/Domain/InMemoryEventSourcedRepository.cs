using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eventing.Core.Domain
{
    public class InMemoryEventSourcedRepository : IEventSourcedRepository
    {
        private readonly object lockObject = new object();
        protected Dictionary<string, List<object>> streams = new Dictionary<string, List<object>>();
        protected Dictionary<string, ISnapshot> snapshots = new Dictionary<string, ISnapshot>();

        public async Task SaveAsync(IEventSourced eventSourced)
        {
            Ensure.NotNullOrWhiteSpace(eventSourced.StreamName, nameof(eventSourced.StreamName));

            lock (this.lockObject)
            {
                if (this.streams.ContainsKey(eventSourced.StreamName))
                    this.streams.Add(eventSourced.StreamName, new List<object>());

                this.streams[eventSourced.StreamName].AddRange(eventSourced.NewEvents);
                this.snapshots[eventSourced.StreamName] = eventSourced.TakeSnapshot();
                eventSourced.MarkAsCommited();
            }
            await Task.CompletedTask;
        }

        public async Task<T> GetByIdAsync<T>(string streamId) where T : class, IEventSourced, new()
        {
            var streamName = StreamCategoryAttribute.GetFullStreamName<T>(streamId);
            return await this.GetByNameAsync<T>(streamName);
        }

        public async Task<T> GetByNameAsync<T>(string streamName) where T : class, IEventSourced, new()
        {
            if (!this.streams.ContainsKey(streamName))
                return null;

            var state = new T();
            lock (this.lockObject)
            {
                this.streams[streamName].ForEach(x => state.Apply(x));
            }

            return await Task.FromResult(state);
        }
    }
}
