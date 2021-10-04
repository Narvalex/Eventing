using Infrastructure.EventSourcing;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Snapshotting
{
    public class NoopPersistentSnapshotter : IPersistentSnapshotter
    {
        public void EnqueueWrite(SnapshotData snapshotData)
        {
        }

        public Task<T?> TryGet<T>(string streamName) where T : class, IEventSourced
        {
            return Task.FromResult(default(T));
        }

        public Task<IEventSourced?> TryGet(Type type, string streamName)
        {
            return Task.FromResult(default(IEventSourced));
        }
    }
}
