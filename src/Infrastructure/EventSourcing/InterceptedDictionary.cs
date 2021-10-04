using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.EventSourcing
{
    public class InterceptedDictionary<TKey, TEntity> : IDictionary<TKey, TEntity>
    {
        private readonly IDictionary<TKey, TEntity> dictionary = new Dictionary<TKey, TEntity>();

        private Action<TKey> interception = x => { };

        internal void RegisterInterception(Action<TKey> interception) => this.interception = interception;

        public TEntity this[TKey key] { get => this.dictionary[key]; set => this.dictionary[key] = value; }

        public ICollection<TKey> Keys => this.dictionary.Keys;

        public ICollection<TEntity> Values => this.dictionary.Values;

        public int Count => this.dictionary.Count;

        public bool IsReadOnly => this.dictionary.IsReadOnly;

        public void Add(TKey key, TEntity value)
        {
            this.dictionary.Add(key, value);
            this.interception.Invoke(key);
        }

        public void Add(KeyValuePair<TKey, TEntity> item)
        {
            this.dictionary.Add(item);
            this.interception.Invoke(item.Key);
        }

        public void Clear()
        {
            this.dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TEntity> item)
        {
            return this.dictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return this.dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TEntity>[] array, int arrayIndex)
        {
            this.dictionary.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TEntity>> GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return this.dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TEntity> item)
        {
            return this.dictionary.Remove(item);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TEntity value)
        {
            return this.dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.dictionary).GetEnumerator();
        }
    }
}
