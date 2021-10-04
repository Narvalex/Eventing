using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.EventSourcing
{
    public abstract class SubEntitiesBase<T>
    {
        public SubEntitiesBase(int lastId = 0, InterceptedDictionary<int, T>? list = null)
        {
            this.LastId = lastId;
           this.SetupDictionary(list ?? new InterceptedDictionary<int, T>());
        }

        protected void SetupDictionary(InterceptedDictionary<int, T> list)
        {
            this.List = list;
            this.List.RegisterInterception(this.OnAddingEntity);
        }

        public InterceptedDictionary<int, T> List { get; private set; }

        public IEnumerable<T> AsEnumerable() => this.List.Values;

        public int LastId { get; private set; }

        public int GetNextId() => this.LastId + 1;

        public bool Any() => this.List.Any();

        public int Count() => this.List.Count;

        public bool Any(int id) => this.List.ContainsKey(id);

        public bool Any(Func<T, bool> predicate)
        {
            return this.List.Values.Any(predicate);
        }

        public T this[int index]
        {
            get => this.List[index];
        }

        public T? FirstOrDefault(int id)
        {
            if (this.List.TryGetValue(id, out var value))
                return value;
            else
                return default(T);
        }

        public T? FirstOrDefault() => this.List.Values.FirstOrDefault();

        public T First() => this.List.First().Value;

        public T First(int id)
        {
            if (this.List.TryGetValue(id, out var value))
                return value;
            else
                throw new InvalidOperationException("Not found sub entity with id " + id);
        }

        public T Last() => this.List.Values.Last();

        public T? LastOrDefault() => this.List.Values.LastOrDefault();

        protected virtual void OnAddingEntity(int entityId)
        {
            if (this.LastId > entityId)
                return;

            this.LastId = entityId;
        }
    }
}
