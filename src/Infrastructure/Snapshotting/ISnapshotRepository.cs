using Infrastructure.EventSourcing;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Snapshotting
{
    public interface ISnapshotRepository
    {
        /// <summary>
        /// Cache a snapshot. Be sure that the snapshot has been taken.
        /// </summary>
        void Save(IEventSourced snapshot);

        void SaveIfNotExists(IEventSourced snapshot);

        bool ExistsInMemory(string streamName);

        /// <summary>
        /// Tries to fetch the snapshot if it is found.
        /// </summary>
        bool TryGetFromMemory<T>(string streamName, out T? snapshot) where T : class, IEventSourced;

        bool TryGetFromMemory(Type type, string streamName, out IEventSourced? snapshot);

        Task<T?> TryGetFromPersistentRepository<T>(string streamName) where T : class, IEventSourced;

        Task<IEventSourced?> TryGetFromPersistentRepository(Type type, string streamName);

        void InvalidateInMemorySnapshot(string streamName);
    }
}
