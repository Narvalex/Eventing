using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.EventSourcing
{
    public abstract class StrSubEntitiesBase<T>
    {
        public StrSubEntitiesBase(InterceptedDictionary<string, T>? list = null)
        {
            this.List = list ?? new InterceptedDictionary<string, T>();
            this.List.RegisterInterception(this.OnAddingEntity);
        }

        public InterceptedDictionary<string, T> List { get; protected set; }

        public IEnumerable<T> AsEnumerable() => this.List.Values;

        public bool Any(string id) => this.List.ContainsKey(id);

        public bool Any(Func<T, bool> predicate)
        {
            return this.List.Values.Any(predicate);
        }

        public T this[string index]
        {
            get => this.List[index];
        }

        protected virtual void OnAddingEntity(string entityId) { }
    }
}
