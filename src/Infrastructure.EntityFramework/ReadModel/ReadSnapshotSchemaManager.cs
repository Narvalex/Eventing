using Infrastructure.EventSourcing;
using Infrastructure.Logging;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.ReadModel
{
    internal class ReadSnapshotSchemaManager<TEventSourced, TDbContext>
        where TEventSourced : class, IEventSourced
        where TDbContext : ReadModelWithSnapshotsDbContext
    {
        private readonly ILogLite log = LogManager.GetLoggerFor("ReadSnapshotSchemaManager-" + typeof(TEventSourced).Name);
        private readonly Func<TDbContext> dbContextFactory;

        internal ReadSnapshotSchemaManager(Func<TDbContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory.EnsuredNotNull(nameof(dbContextFactory));
        }

        public int SchemaVersion { get; private set; }

        public async Task InitializeSchema()
        {
            // 1. Compare hash schemes, check if stale are true
            // 2. If no change detected, return
            // 2.1. Update hash in SQL and set found stale to true
            var (changesDetected, schemaVersion) = await this.DetectSchemaChangesOrPendingStaleSnapshots();
            if (changesDetected)
                this.log.Warning("Changes where detected in schema. Updates on snapshots will be applied on demmand");
            else
                this.log.Verbose("No snapshot schema updates detected");

            this.SchemaVersion = schemaVersion;
        }

        private async Task<(bool changesDetected, int schemaVersion)> DetectSchemaChangesOrPendingStaleSnapshots()
        {
            var eventSourced = EventSourcedCreator.New<TEventSourced>();
            var actualHash = EventSourcedEntityHasher.GetHash<TEventSourced>();
            using (var context = this.dbContextFactory())
            {
                var dbSchemaEntity = await context.SnapshotSchemas.FirstOrDefaultAsync(x => x.Type == eventSourced.GetEntityType());
                if (dbSchemaEntity is null)
                {
                    // Scheme is brandnew
                    context.SnapshotSchemas.Add(new SnapshotSchemaEntity
                    {
                        Assembly = eventSourced.GetAssembly(),
                        Hash = actualHash,
                        Type = eventSourced.GetEntityType(),
                        Version = 0
                    });

                    await context.UnsafeSaveChangesAsync();
                    return (false, 0);
                }
                else
                {
                    if (actualHash.IsEqualWithOrdinalIgnoreCaseComparisson(dbSchemaEntity.Hash))
                    {
                        return (false, dbSchemaEntity.Version);
                    }
                    else
                    {
                        dbSchemaEntity.Hash = actualHash;
                        dbSchemaEntity.Version = dbSchemaEntity.Version + 1;

                        await context.UnsafeSaveChangesAsync();
                        return (true, dbSchemaEntity.Version);
                    }
                }
            }
        }
    }
}
