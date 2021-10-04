using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Utils
{
    public static class AsyncStreamsExtensions
    {
        // It is not called "Any" because of a collition with EF
        public static async Task<bool> HasAnyAsync<T>(this IAsyncEnumerable<T> stream, Func<T, bool> predicate)
        {
            await foreach (var item in stream)
            {
                if (predicate.Invoke(item))
                    return true;
                else
                    continue;
            }

            return false;
        }

        // It is not called "FirstOrDefaultAsync" because of a collition with EF
        public static async Task<T?> GetFirstOrDefaultAsync<T>(this IAsyncEnumerable<T> stream, Func<T, bool> predicate) where T : class
        {
            await foreach (var item in stream)
            {
                if (predicate.Invoke(item))
                    return item;
                else
                    continue;
            }

            return null;
        }

        public static async Task<T> GetFirstAsync<T>(this IAsyncEnumerable<T> stream, Func<T, bool> predicate) where T : class
        {
            await foreach (var item in stream)
            {
                if (predicate.Invoke(item))
                    return item;
                else
                    continue;
            }

            throw new InvalidOperationException("Could not get first element mathing the predicate");
        }

        // It is not called "Where" because of a collition with EF
        public static async IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> stream, Func<T, bool> predicate)
        {
            await foreach (var item in stream)
            {
                if (predicate.Invoke(item))
                    yield return item;
                else
                    continue;
            }
        }

        // It is not called "Where" because of a collition with EF
        public static async IAsyncEnumerable<T> FilterAsync<T>(this IAsyncEnumerable<T> stream, Func<T, Task<bool>> predicate)
        {
            await foreach (var item in stream)
            {
                if (await predicate.Invoke(item))
                    yield return item;
                else
                    continue;
            }
        }

        // It is not called "Select" because of a collition with EF
        public static async IAsyncEnumerable<TResult> Map<TSource, TResult>(this IAsyncEnumerable<TSource> stream, Func<TSource, TResult> selector)
        {
            await foreach (var item in stream)
            {
                yield return selector(item);
            }
        }

        // It is not called "Select" because of a collition with EF
        public static async IAsyncEnumerable<TResult> MapAsync<TSource, TResult>(this IAsyncEnumerable<TSource> stream, Func<TSource, Task<TResult>> selector)
        {
            await foreach (var item in stream)
            {
                yield return await selector(item);
            }
        }

        public static async Task ForEach<T>(this IAsyncEnumerable<T> stream, Action<T> action)
        {
            await foreach (var item in stream)
            {
                action.Invoke(item);
            }
        }

        public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> stream, Func<T, Task> function)
        {
            await foreach (var item in stream)
            {
                await function.Invoke(item);
            }
        }

        public static async IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> stream, int count)
        {
            var current = 0;
            await foreach (var item in stream)
            {
                current += 1;
                if (current <= count)
                    yield return item;
                else
                    break;
            }
        }

        // It is not called "ToListAsync" because of a collition with EF
        public static async Task<List<T>> ToListFromStreamAsync<T>(this IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (var item in source)
            {
                list.Add(item);
            }

            return list;
        }

        public static async Task<int> CountAsync<TSource>(this IAsyncEnumerable<TSource> source)
        {
            var count = 0;
            await foreach (var item in source)
            {
                count += 1;
            }

            return count;
        }

        public static async Task<long> LongCountAsync<TSource>(this IAsyncEnumerable<TSource> source)
        {
            long count = 0;
            await foreach (var item in source)
            {
                count += 1;
            }

            return count;
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
        public static IAsyncEnumerable<TSource> Distinct<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
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
        public static async IAsyncEnumerable<TSource> Distinct<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            Ensure.NotNull(source, nameof(source));
            Ensure.NotNull(keySelector, nameof(keySelector));

            var knownKeys = new HashSet<TKey>(comparer);
            await foreach (var element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                    yield return element;
            }
        }

        #endregion

    }
}
