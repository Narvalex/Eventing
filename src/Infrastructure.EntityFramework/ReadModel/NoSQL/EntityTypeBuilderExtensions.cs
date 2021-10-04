using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityFramework.ReadModel.NoSQL
{
    public static class EntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder<TEntity> ToDocumentCollection<TEntity>(this EntityTypeBuilder<TEntity> builder, string collectionName) where TEntity : class
        {
            return builder.ToTable(collectionName, "doc");
        }
    }
}
