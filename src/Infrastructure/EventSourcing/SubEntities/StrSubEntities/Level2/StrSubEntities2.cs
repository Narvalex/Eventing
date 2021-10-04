namespace Infrastructure.EventSourcing
{
    public sealed class StrSubEntities2<T> : StrSubEntitiesBase<T>
       where T : StrSubEntity2Base
    {
        public StrSubEntities2(InterceptedDictionary<string, T>? list = null)
            : base(list)
        {
        }

        public void Add(T subEntity)
        {
            this.List.Add(subEntity.Id, subEntity);
        }

        public StrSubEntities2<T> Remove(string id)
        {
            this.List.Remove(id);
            return this;
        }
    }
}
