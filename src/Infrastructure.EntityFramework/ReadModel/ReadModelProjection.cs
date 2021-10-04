using Infrastructure.EntityFramework.ReadModel.NoSQL;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.ReadModel
{

    public abstract class ReadModelProjection<TEventSourced, TEntity, TDbContext> : BasicReadModelProjection<TDbContext>, IReadModelSnapshotsSchemaInitializer
        where TEventSourced : class, IEventSourced
        where TEntity : SnapshotEntity<TEventSourced>, new()
        where TDbContext : ReadModelWithSnapshotsDbContext
    {
        private readonly Func<TDbContext, DbSet<TEntity>> snapshotCollectionSelector;
        private readonly ReadSnapshotSchemaManager<TEventSourced, TDbContext> schemaManager;
        private int pageSize = 500;

        protected ReadModelProjection(
            IEfReadModelProjector<TDbContext> efReadModelProjector,
            Func<TDbContext, DbSet<TEntity>> snapshotCollectionSelector)
            : base(efReadModelProjector)
        {
            // Setup Sanpshot Migrator
            ReadModelSanpshotMigrator.Setup(efReadModelProjector.EventStore, this.pageSize);

            this.snapshotCollectionSelector = snapshotCollectionSelector.EnsuredNotNull(nameof(snapshotCollectionSelector));
            this.schemaManager = new ReadSnapshotSchemaManager<TEventSourced, TDbContext>(
                efReadModelProjector.DbContextFactory
            );

            var genericHandler = typeof(IEventHandler<>);
            var projectedEventTypes = this.GetType()
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Transform(d => d == genericHandler))
                .Select(i => i.GetGenericArguments()[0])
                .ToList();

            var requiredEventTypes = EventSourcedCreator.New<TEventSourced>().GetSourcingEventTypes();

            if (!requiredEventTypes.All(x => projectedEventTypes.Contains(x)))
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                requiredEventTypes.ForEach(x =>
                {
                    if (!projectedEventTypes.Contains(x))
                        sb.AppendLine(x.Name);
                });
                throw new InvalidOperationException($"The {this.GetType().Name} does not handle all events from {typeof(TEventSourced).Name}. Missing events: " + sb.ToString());
            }
        }

        protected override Task Reduce(IEvent e, Func<TDbContext, Task> function) =>
            base.Reduce(e, async context =>
            {
                await this.UpdateSnapshot(this.snapshotCollectionSelector(context), e, this.schemaManager.SchemaVersion);
                await function(context);
            });

        protected Task OnlyUpdateSnapshot(IEvent e) =>
             base.Reduce(e, async context =>
             {
                 await this.UpdateSnapshot(this.snapshotCollectionSelector(context), e, this.schemaManager.SchemaVersion);
             });

        private async Task UpdateSnapshot(DbSet<TEntity> dbSet, IEvent e, int upToDateSchemaVersion)
        {
            var entity = await dbSet.FirstOrDefaultFromLocalAsync(x => x.StreamId == e.StreamId);
            if (entity is null)
            {
                entity = new TEntity();
                entity.StreamId = e.StreamId;
                var snapshot = EventSourcedCreator.New<TEventSourced>();
                snapshot.Apply(e);
                snapshot.ApplyOutputState();
                entity.Prepare(snapshot);
                entity.Version = snapshot.Metadata.Version;
                entity.SchemaVersion = upToDateSchemaVersion;
                dbSet.Add(entity);
            }
            else if (entity.SchemaVersion == upToDateSchemaVersion)
            {
                var snapshot = entity.InternalOpen();
                snapshot.Apply(e);
                snapshot.ApplyOutputState();
                entity.Prepare(snapshot);
                entity.Version = snapshot.Metadata.Version;
                // the schema is the same
            }
            else 
            {
                // snapshot is outdated
                await ReadModelSanpshotMigrator.Migrate(entity, upToDateSchemaVersion, e);
            }
        }

        Task IReadModelSnapshotsSchemaInitializer.InitializeReadModelSnapshotSchema() => 
            this.schemaManager.InitializeSchema();
    }
}