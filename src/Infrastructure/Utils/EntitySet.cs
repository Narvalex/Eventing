using System;
using System.Collections;
using System.Collections.Generic;

namespace Infrastructure.Utils
{
    public class EntitySet<T> : ICollection<T>
    {
        private readonly HashSet<T> hashSet;

        public EntitySet()
        {
            this.hashSet = new HashSet<T>();
        }

        public EntitySet(IEnumerable<T> collection)
        {
            this.hashSet = new HashSet<T>(collection);
        }

        public int Count => this.hashSet.Count;

        public bool IsReadOnly => ((ICollection<T>)this.hashSet).IsReadOnly;

        public void Add(T item)
        {
            if (!this.hashSet.Add(item))
                throw new ArgumentException($"The item of type {item.GetType()} already exists in entity set.");
        }

        public void Clear()
        {
            this.hashSet.Clear();
        }

        public bool Contains(T item)
        {
            return this.hashSet.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.hashSet.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.hashSet.GetEnumerator();
        }

        public bool Remove(T item)
        {
            return this.hashSet.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.hashSet.GetEnumerator();
        }

        public int RemoveWhere(Predicate<T> predicate)
        {
            return this.hashSet.RemoveWhere(predicate);
        }
    }
}
