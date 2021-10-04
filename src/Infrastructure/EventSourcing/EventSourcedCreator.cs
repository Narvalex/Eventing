using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Infrastructure.EventSourcing
{
    public static class EventSourcedCreator
    {
        private static ConcurrentDictionary<Type, Func<object>> factories = new ConcurrentDictionary<Type, Func<object>>();

        /// <summary>
        /// Creates an new empty instance of <see cref="T"/>
        /// </summary>
        /// <typeparam name="T">The type of the event sourced instance.</typeparam>
        /// <returns>A new empty instance of <see cref="T"/></returns>
        public static T New<T>() where T : IEventSourced
        {
            var eventSourced = (T)New(typeof(T));
            return eventSourced;
        }


        public static IEventSourced New(Type type)
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

                // Default version of long is 0 and version is 1
                parameters[0] = new EventSourcedMetadata(null!, EventStream.NoEventsNumber, EventStream.NoEventsNumber, false, false, null);

                factories[type] = () => constructor.Invoke(parameters);
                return constructor.Invoke(parameters);
            });
            return (IEventSourced)factory.Invoke();
        }
    }
}
