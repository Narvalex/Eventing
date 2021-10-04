using Infrastructure.DateTimeProvider;
using Infrastructure.EntityFramework.Snapshotting.Database;
using Infrastructure.Snapshotting;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.Snapshotting
{
    public class EfSnapshotStore : ISnapshotStore
    {
        private readonly Func<SnapshotStoreDbContext> writeContextFactory;
        private readonly Func<SnapshotStoreDbContext> readContextFactory;
        private readonly IDateTimeProvider dateTime;

        public EfSnapshotStore(Func<SnapshotStoreDbContext> writeContextFactory, Func<SnapshotStoreDbContext> readContextFactory, IDateTimeProvider dateTime)
        {
            this.writeContextFactory = writeContextFactory.EnsuredNotNull(nameof(writeContextFactory));
            this.readContextFactory = readContextFactory.EnsuredNotNull(nameof(readContextFactory));
            this.dateTime = dateTime.EnsuredNotNull(nameof(dateTime));
        }

        public async Task<IList<SnapshotSchema>> GetSchemas()
        {
            using (var context = this.readContextFactory())
            {
                var list = await EntityFrameworkQueryableExtensions.ToListAsync(context.Schemas);
                return list
                        .Select(x => new SnapshotSchema(x.Type, x.Assembly, x.Version, x.Hash, x.ThereAreStaleSnapshots))
                        .ToList();
            }
        }

        public async Task<SnapshotData?> TryGetFirstStaleSnapshot(string type, int schemaVersion)
        {
            using (var context = this.readContextFactory())
            {
                var entity = await context.Snapshots.FirstOrDefaultAsync(x => x.Type == type && x.SchemaVersion != schemaVersion);
                if (entity is null)
                    return null;

                return new SnapshotData(entity.StreamName, entity.Version, entity.Payload, entity.Type, entity.Assembly, schemaVersion);
            }
        }

        public async Task<SnapshotData?> TryGetSnapshot(string streamName, int schemaVersion)
        {
            using (var context = this.readContextFactory())
            {
                var entity = await context.Snapshots.FirstOrDefaultAsync(x => x.StreamName == streamName && x.SchemaVersion == schemaVersion);
                if (entity is null)
                    return null;

                return new SnapshotData(entity.StreamName, entity.Version, entity.Payload, entity.Type, entity.Assembly, schemaVersion);
            }
        }

        public async Task Save(params SnapshotSchema[] schemas)
        {
            using (var context = this.writeContextFactory())
            {
                foreach (var s in schemas)
                {
                    var entity = await context.Schemas.FirstOrDefaultAsync(x => x.Type == s.Type);
                    if (entity is null)
                    {
                        context.Schemas.Add(new SchemaEntity
                        {
                            Type = s.Type,
                            Assembly = s.Assembly,
                            Hash = s.Hash,
                            Version = s.Version,
                            ThereAreStaleSnapshots = s.ThereAreStaleSnapshots
                        });
                    }
                    else
                    {
                        entity.Hash = s.Hash;
                        entity.Version = s.Version;
                        entity.ThereAreStaleSnapshots = s.ThereAreStaleSnapshots;
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task Save(params SnapshotData[] snapshots)
        {
            var now = this.dateTime.Now;
            using (var context = this.writeContextFactory())
            {
                foreach (var s in snapshots)
                {
                    var entity = await context.Snapshots.FirstOrDefaultAsync(x => x.StreamName == s.StreamName);
                    if (entity is null)
                    {
                        context.Snapshots.Add(new SnapshotEntity
                        {
                            StreamName = s.StreamName,
                            Type = s.Type,
                            Version = s.Version,
                            Assembly = s.Assembly,
                            Payload = s.Payload,
                            SchemaVersion = s.SchemaVersion,
                            Timestamp = now,
                            Size = s.Size
                        });
                    }
                    else
                    {
                        entity.Payload = s.Payload;
                        entity.Version = s.Version;
                        entity.SchemaVersion = s.SchemaVersion;
                        entity.Timestamp = now;
                        entity.Size = s.Size;
                    }
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
