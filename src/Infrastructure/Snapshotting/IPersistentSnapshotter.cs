using Infrastructure.EventSourcing;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Snapshotting
{
    public interface IPersistentSnapshotter
    {
        void EnqueueWrite(SnapshotData snapshotData);

        Task<T?> TryGet<T>(string streamName) where T : class, IEventSourced;

        Task<IEventSourced?> TryGet(Type type, string streamName);
    }
}
