using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.EntityFramework.Snapshotting.Database
{
    public class SnapshotStoreDbContext : DbContext
    {
        public SnapshotStoreDbContext(DbContextOptions<SnapshotStoreDbContext> options, bool autoDetectChanges)
            : base(options)
        {
            this.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }

        public DbSet<SchemaEntity> Schemas { get; set; }
        public DbSet<SnapshotEntity> Snapshots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .ApplyConfiguration(new SchemaEntityConfig())
                .ApplyConfiguration(new SnapshotEntityConfig())
            ;
        }

    }
}
