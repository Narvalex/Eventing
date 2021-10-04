using Infrastructure.EntityFramework.ReadModel;
using Infrastructure.EntityFramework.ReadModel.NoSQL;
using Infrastructure.EventSourcing;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework
{
    public static class SnapshotEntityDbSetExtensions
    {
        private readonly static ConcurrentDictionary<(Type, Type), int> schemaVersionsByEsTypeAndDbContextType = new ConcurrentDictionary<(Type, Type), int>();

        public static async Task<TEventSourced?> FirstOrDefaultFromLocalAsync<TEventSourced>(this DbSet<SnapshotEntity<TEventSourced>> dbSet, string streamId)
            where TEventSourced : class, IEventSourced
        {
            var entity = dbSet.Local.FirstOrDefault(x => x.StreamId == streamId) ?? await dbSet.FirstOrDefaultAsync(x => x.StreamId == streamId);
            if (entity is null) return null;
            return await ResolveEventSourced(entity, dbSet);
        }

        public static async Task<TEventSourced> FirstFromLocalAsync<TEventSourced>(this DbSet<SnapshotEntity<TEventSourced>> dbSet, string streamId)
           where TEventSourced : class, IEventSourced
        {
            var entity = dbSet.Local.FirstOrDefault(x => x.StreamId == streamId);
            if (entity is null)
            {
                entity = await dbSet.FirstAsync(x => x.StreamId == streamId);
            }

            return await ResolveEventSourced(entity, dbSet);
        }

        public static Task<TEventSourced> FirstFromLocalAsync<TEventSourced>(this DbSet<SnapshotEntity<TEventSourced>> dbSet, Func<TEventSourced, bool> predicate)
          where TEventSourced : class, IEventSourced =>
          dbSet.ToSnapshotEnumerableFromLocalAsync().Then(s => s.First(predicate));

        public static async Task<IEnumerable<TEventSourced>> ToSnapshotEnumerableFromLocalAsync<TEventSourced>(this DbSet<SnapshotEntity<TEventSourced>> dbSet)
            where TEventSourced : class, IEventSourced
        {
            var listFromDisk = await dbSet.ToListAsync();
            var listFromMemory = dbSet.Local.ToList();
            listFromMemory.AddRange(listFromDisk.Where(x => !listFromMemory.Any(m => m.StreamId == x.StreamId)));
            return await listFromMemory.SelectAsync(x => ResolveEventSourced(x, dbSet));
        }

        private static async Task<TEventSourced> ResolveEventSourced<TEventSourced>(SnapshotEntity<TEventSourced> entity, DbSet<SnapshotEntity<TEventSourced>> dbSet)
            where TEventSourced : class, IEventSourced
        {
            var esType = typeof(TEventSourced);
            var dbContext = dbSet.GetService<ICurrentDbContext>().Context;
            var updatedSchemaVersion = schemaVersionsByEsTypeAndDbContextType.GetOrAdd((esType, dbContext.GetType()), _ =>
            {
                var context = (ReadModelWithSnapshotsDbContext)dbContext;
                var schema = context.SnapshotSchemas.FirstOrDefault(x => x.Type == esType.FullName);
                return schema?.Version ?? 0;
            });

            if (entity.SchemaVersion == updatedSchemaVersion)
                return entity.InternalOpen();

            return await ReadModelSanpshotMigrator.Migrate(entity, updatedSchemaVersion);
        }
    }
}
