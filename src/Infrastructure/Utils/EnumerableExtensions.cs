using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Utils
{
    /// <summary>
    /// Usability extensions for enumerables.
    /// </summary>
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
                action.Invoke(item);
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> collection, Func<T, Task> func)
        {
            foreach (var item in collection)
                await func.Invoke(item);
        }

        public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(this IEnumerable<TSource> collection, Func<TSource, Task<TResult>> func)
        {
            var list = new List<TResult>();
            foreach (var item in collection)
            {
                var result = await func.Invoke(item);
                list.Add(result);
            }

            return list;
        }

        public static async Task<IEnumerable<TSource>> WhereAsync<TSource>(this Task<IEnumerable<TSource>> collection, Func<TSource, bool> func)
        {
            var list = new List<TSource>();
            foreach (var item in await collection)
            {
                if (func.Invoke(item))
                    list.Add(item);
            }

            return list;
        }

        public static async Task<List<T>> ToListAsync<T>(this Task<IEnumerable<T>> enumerable)
        {
            return (await enumerable).ToList();
        }

        public static async Task<T[]> ToArrayAsync<T>(this Task<IEnumerable<T>> enumerable)
        {
            return (await enumerable).ToArray();
        }

        public static EntitySet<T> ToEntitySet<T>(this IEnumerable<T> collection)
        {
            var set = new EntitySet<T>();
            set.AddRange(collection);
            return set;
        }

        public static async Task<TResult> FoldAsync<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, Task<TAccumulate>> func, Func<TAccumulate, TResult> resultSelector)
        {
            seed = await source.FoldAsync(seed, func);

            return resultSelector(seed);
        }

        public static async Task<TAccumulate> FoldAsync<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, Task<TAccumulate>> func)
        {
            foreach (var item in source)
            {
                seed = await func(seed, item);
            }

            return seed;
        }

        #region Distinct

        // Thanks to: https://github.com/morelinq/MoreLINQ/blob/master/MoreLinq/DistinctBy.cs
        // And: https://stackoverflow.com/questions/489258/linqs-distinct-on-a-particular-property

        /// <summary>
        /// Returns all distinct elements of the given source, where "distinctness"
        /// is determined via a projection and the default equality comparer for the projected type.
        /// </summary>
        /// <remarks>
        /// This operator uses deferred execution and streams the results, although
        /// a set of already-seen keys is retained. If a key is seen multiple times,
        /// only the first element with that key is returned.
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="keySelector">Projection for determining "distinctness"</param>
        /// <returns>A sequence consisting of distinct elements from the source sequence,
        /// comparing them by the specified key projection.</returns>
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.Distinct(keySelector, null);
        }

        /// <summary>
        /// /// <summary>
        /// Returns all distinct elements of the given source, where "distinctness"
        /// is determined via a projection and the specified comparer for the projected type.
        /// </summary>
        /// <remarks>
        /// This operator uses deferred execution and streams the results, although
        /// a set of already-seen keys is retained. If a key is seen multiple times,
        /// only the first element with that key is returned.
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="keySelector">Projection for determining "distinctness"</param>
        /// <param name="comparer">The equality comparer to use to determine whether or not keys are equal.
        /// If null, the default equality comparer for <c>TSource</c> is used.</param>
        /// <returns>A sequence consisting of distinct elements from the source sequence,
        /// comparing them by the specified key projection.</returns>
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            Ensure.NotNull(source, nameof(source));
            Ensure.NotNull(keySelector, nameof(keySelector));

            return _(); IEnumerable<TSource> _()
            {
                var knownKeys = new HashSet<TKey>(comparer);
                foreach (var element in source)
                {
                    if (knownKeys.Add(keySelector(element)))
                        yield return element;
                }
            }
        }

        #endregion
    }
}
