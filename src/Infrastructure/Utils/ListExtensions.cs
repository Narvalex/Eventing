using System.Collections.Generic;

namespace Infrastructure.Utils
{
    public static class ListExtensions
    {
        public static IList<T> AddIt<T>(this IList<T> list, T item)
        {
            list.Add(item);
            return list;
        }

        public static IList<T> RemoveIt<T>(this IList<T> list, T item)
        {
            list.Remove(item);
            return list;
        }
    }
}
