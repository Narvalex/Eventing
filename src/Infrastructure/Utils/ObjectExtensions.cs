using System;
using System.Threading.Tasks;

namespace Infrastructure.Utils
{
    public static class ObjectExtensions
    {
        public static TResult Transform<TSource, TResult>(this TSource source, Func<TSource, TResult> predicate)
            => predicate.Invoke(source);

        public static Task<TResult> TransformAsync<TSource, TResult>(this TSource source, Func<TSource, Task<TResult>> predicate)
            => predicate.Invoke(source);

        public static TTarget Tap<TTarget>(this TTarget target, Action<TTarget> interception)
        {
            interception.Invoke(target);
            return target;
        }

        public static async Task<TTarget> TapAsync<TTarget>(this TTarget target, Func<TTarget, Task> predicate)
        {
            await predicate(target);
            return target;
        }

        public static T EnsuredNotNull<T>(this T? parameter, string parameterName) where T : class
        {
            return Ensured.NotNull<T>(parameter, parameterName);
        }
    }
}
