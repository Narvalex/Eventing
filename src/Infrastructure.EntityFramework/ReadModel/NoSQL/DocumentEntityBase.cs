using System;

namespace Infrastructure.EntityFramework.ReadModel.NoSQL
{
    public abstract class DocumentEntityBase<T> where T : class
    {
        public string Document { get; set; } = null!;
        public DateTime Timestamp { get; set; }

        public void Prepare(T document)
        {
            this.Timestamp = DocumentEntityDependenciesConfig.Timestamp.Now;
            this.Document = DocumentEntityDependenciesConfig.Serializer.SerializeToPlainJSON(document);
        }

        public virtual T Open() => DocumentEntityDependenciesConfig.Serializer.Deserialize<T>(this.Document);
    }
}
