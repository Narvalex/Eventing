using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Infrastructure.Utils
{
    public static class ObjectCreator
    {
        private static ConcurrentDictionary<Type, Func<object>> factories = new ConcurrentDictionary<Type, Func<object>>();

        public static T New<T>()
        {
            var entity = (T)New(typeof(T));
            return entity;
        }

        public static object New(Type type)
        {
            var factory = factories.GetOrAdd(type, () =>
            {
                // the performance of this approach was tested aggainst having a cache of serialized EventSourced entities
                // this was faster (75 ms for 10.000 instances on single thread vs 208 ms)
                var constructor = type.GetConstructors()[0];
                var parameters = constructor
                                    .GetParameters()
                                    .Select(x =>
                                        x.ParameterType.IsValueType
                                        ? Activator.CreateInstance(x.ParameterType)
                                        : null)
                                    .ToArray();

                factories[type] = () => constructor.Invoke(parameters);
                return constructor.Invoke(parameters);
            });
            return factory.Invoke();
        }
    }
}
