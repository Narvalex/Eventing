using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.EntityFramework.Snapshotting.Database
{
    public class SnapshotEntity
    {
        public string StreamName { get; set; }
        public long Version { get; set; }
        public string Payload { get; set; }
        public string Type { get; set; }
        public string Assembly { get; set; }
        public int SchemaVersion { get; set; }
        public DateTime Timestamp { get; set; }
        public long Size { get; set; }
    }

    public class SnapshotEntityConfig : IEntityTypeConfiguration<SnapshotEntity>
    {
        public void Configure(EntityTypeBuilder<SnapshotEntity> builder)
        {
            builder.HasKey(x => x.StreamName);
            builder.HasIndex(x => new { x.Type, x.SchemaVersion });
            builder.ToTable("Snapshots");
        }
    }
}
