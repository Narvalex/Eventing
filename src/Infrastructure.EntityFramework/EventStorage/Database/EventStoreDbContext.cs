using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.EntityFramework.EventStorage.Database
{
    public class EventStoreDbContext : DbContext
    {
        public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options, bool autoDetectChanges)
            : base(options)
        {
            this.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }

        public static EventStoreDbContext ResolveNewInMemoryContext(string databaseName)
            => new EventStoreDbContext(new DbContextOptionsBuilder<EventStoreDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options, true);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new EventDescriptorConfig());
        }

        public DbSet<EventDescriptor> Events { get; set; }
    }

    public class EventDescriptor
    {
        public long Position { get; set; }

        public string Category { get; set; }

        public string SourceId { get; set; }

        public long Version { get; set; }

        public string EventType { get; set; }

        public DateTime TimeStamp { get; set; }

        public string Payload { get; set; }

        public string Metadata { get; set; }
    }

    public class EventDescriptorConfig : IEntityTypeConfiguration<EventDescriptor>
    {
        public void Configure(EntityTypeBuilder<EventDescriptor> builder)
        {
            builder.HasKey(x => new { x.Category, x.SourceId, x.Version });
            builder.HasIndex(x => x.Position);
            builder.ToTable("Events");
        }
    }
}
