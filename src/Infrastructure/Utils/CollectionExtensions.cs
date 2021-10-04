using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Utils
{
    /// <summary>
    /// Usability extensions for collections.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Adds a set of items to a collection.
        /// </summary>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        public static ICollection<T> AddNow<T>(this ICollection<T> collection, T item)
        {
            collection.Add(item);
            return collection;
        }
        
        public static ICollection<T> AddRangeNow<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            collection.AddRange(items);
            return collection;
        }

        public static List<T> AddRangeNow<T>(this List<T> collection, IEnumerable<T> items)
        {
            collection.AddRange(items);
            return collection;
        }

        public static bool Remove<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            return collection.Remove(collection.FirstOrDefault(predicate));
        }

        // Thanks to: http://codemyne.net/Articles/2012/8/Flat-Data-to-Hierarchical-Data-using-CSharp-and-Linq
        // And: https://stackoverflow.com/questions/19648166/nice-universal-way-to-convert-list-of-items-to-tree
        public static ICollection<IHierarchicalItem<T>> ToHierarchicalCollection<T, K>(
            this ICollection<T> collection,
            Func<T, K> idSelector,
            Func<T, K> parentIdSelector,
            K rootId = default)
        {
            var recursiveObjects = new List<IHierarchicalItem<T>>();
            foreach (var i in collection.Where(x => 
                EqualityComparer<K>.Default.Equals(parentIdSelector(x), rootId))) // To avoid NRE
            {
                recursiveObjects.Add(new HierarchicalItem<T>
                {
                    Item = i,
                    Children = collection.ToHierarchicalCollection(idSelector, parentIdSelector, idSelector(i))
                });
            }

            return recursiveObjects;
        }
    }
}
