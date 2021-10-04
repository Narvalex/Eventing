namespace Infrastructure.EntityFramework.ReadModel.NoSQL
{
    public static class DocumentEntityExtensions
    {
        public static TEntity Store<TEntity, TDocument>(this TEntity entity, TDocument document) where TEntity : DocumentEntity<TDocument> where TDocument : class
        {
            entity.Prepare(document);
            return entity;
        }
    }
}
