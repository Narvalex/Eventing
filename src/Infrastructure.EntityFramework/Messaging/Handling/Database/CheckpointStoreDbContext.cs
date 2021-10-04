using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.EntityFramework.Messaging.Handling.Database
{
    public class CheckpointStoreDbContext : DbContext
    {
        public CheckpointStoreDbContext(DbContextOptions<CheckpointStoreDbContext> options, bool autoDetectChanges) : base(options)
        {
            this.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }

        // More info: https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory
        public static CheckpointStoreDbContext ResolveInMemoryContext(string databaseName)
            => new CheckpointStoreDbContext(new DbContextOptionsBuilder<CheckpointStoreDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options, true);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new SubscriptionCheckpointConfig());
        }

        public DbSet<CheckpointEntity> Checkpoints { get; set; }
    }

    public class CheckpointEntity
    {
        public string SubscriptionId { get; set; }
        public string Type { get; set; }
        public long EventNumber { get; set; }
        public long CommitPosition { get; set; }
        public long PreparePosition { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class SubscriptionCheckpointConfig : IEntityTypeConfiguration<CheckpointEntity>
    {
        public void Configure(EntityTypeBuilder<CheckpointEntity> builder)
        {
            builder.HasKey(x => x.SubscriptionId);
            builder.ToTable("Checkpoints");
        }
    }
}
