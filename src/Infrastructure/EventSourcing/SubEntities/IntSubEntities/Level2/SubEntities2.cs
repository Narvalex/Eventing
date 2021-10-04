using System;
using System.Linq;

namespace Infrastructure.EventSourcing
{
    public sealed class SubEntities2<T> : SubEntitiesBase<T> 
        where T : SubEntity2Base
    {
        public SubEntities2(int lastId = 0, InterceptedDictionary<int, T>? list = null) 
            : base(lastId, list)
        {
        }

        public void Add(T subEntity2)
        {
            this.List.Add(subEntity2.Id, subEntity2);
        }

        public SubEntities2<T> Remove(int id)
        {
            this.List.Remove(id);
            return this;
        }
    }
}
