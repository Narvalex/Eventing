#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.EntityFramework.ReadModel
{
    public abstract class ReadModelWithSnapshotsDbContext : ReadModelDbContext
    {
        protected ReadModelWithSnapshotsDbContext([NotNull] DbContextOptions options, bool autoDetectChanges) 
            : base(options, autoDetectChanges)
        {
        }

        public DbSet<SnapshotSchemaEntity> SnapshotSchemas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new SnapshotSchemaEntityConfig());
        }
    }
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
