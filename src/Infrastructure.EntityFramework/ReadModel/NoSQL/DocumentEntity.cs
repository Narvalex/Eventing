namespace Infrastructure.EntityFramework.ReadModel.NoSQL
{
    public abstract class DocumentEntity<T> : DocumentEntityBase<T> where T : class
    {
        public static implicit operator T?(DocumentEntity<T>? document)
        {
            return document?.Open();
        }
    }
}
