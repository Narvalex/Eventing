using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityFramework.Snapshotting.Database
{
    public class SchemaEntity
    {
        public string Type { get; set; }
        public string Assembly { get; set; }
        public int Version { get; set; }
        public bool ThereAreStaleSnapshots { get; set; }
        public string Hash { get; set; }
    }

    public class SchemaEntityConfig : IEntityTypeConfiguration<SchemaEntity>
    {
        public void Configure(EntityTypeBuilder<SchemaEntity> builder)
        {
            builder.HasKey(x => x.Type);
            builder.ToTable("Schemas");
        }
    }
}
