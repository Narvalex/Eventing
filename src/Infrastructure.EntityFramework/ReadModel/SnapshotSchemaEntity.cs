using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityFramework.ReadModel
{
    public class SnapshotSchemaEntity
    {
        public string Type { get; set; }
        public string Assembly { get; set; }
        public int Version { get; set; }
        public string Hash { get; set; }
    }

    public class SnapshotSchemaEntityConfig : IEntityTypeConfiguration<SnapshotSchemaEntity>
    {
        public void Configure(EntityTypeBuilder<SnapshotSchemaEntity> builder)
        {
            builder.HasKey(x => x.Type);
            builder.ToTable("SnapshotSchemas", "snap");
        }
    }
}
