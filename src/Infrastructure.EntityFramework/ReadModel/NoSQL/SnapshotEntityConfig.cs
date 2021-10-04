using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityFramework.ReadModel.NoSQL
{
    public class SnapshotEntityConfig<T> : IEntityTypeConfiguration<T>
        where T : class, ISnapshotEntity
    {
        private readonly string tableName;
        public SnapshotEntityConfig(string tableName)
        {
            this.tableName = tableName;
        }

        public void Configure(EntityTypeBuilder<T> builder)
        {
            builder.ToTable(this.tableName, "snap");
            builder.HasKey(x => x.StreamId);
        }
    }
}
