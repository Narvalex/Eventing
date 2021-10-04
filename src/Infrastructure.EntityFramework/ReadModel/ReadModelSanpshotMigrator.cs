using Infrastructure.EntityFramework.ReadModel.NoSQL;
using Infrastructure.EventSourcing;
using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.ReadModel
{
    internal static class ReadModelSanpshotMigrator
    {
        private static IEventStore? eventStore;
        private static int pageSize;
        internal static void Setup(IEventStore eventStoreParam, int pageSizeParam)
        {
            if (eventStore is not null) return;
            eventStore = eventStoreParam;
            pageSize = pageSizeParam;
        }

        internal static async Task<TEventSourced> Migrate<TEventSourced>(SnapshotEntity<TEventSourced> entity, int upToDateSchemaVersion, IEvent? e = null)
            where TEventSourced : class, IEventSourced
        {
            var snapshot = EventSourcedCreator.New<TEventSourced>();
            await snapshot.TryRehydrate(
                streamName: EventStream.GetStreamName<TEventSourced>(entity.StreamId),
                maxVersion: entity.Version,
                store: eventStore!,
                sliceStart: 0,
                readPageSize: pageSize
            );
            if (e != null)
            {
                snapshot.Apply(e);
                snapshot.ApplyOutputState();
            }

            entity.Prepare(snapshot);
            entity.Version = snapshot.Metadata.Version;
            // Schema updated
            entity.SchemaVersion = upToDateSchemaVersion;
            return snapshot;
        }
    }
}