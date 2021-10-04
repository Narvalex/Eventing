using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public class DynamicEventDispatcher : IEventDispatcher
    {
        private readonly Dictionary<Type, IEventHandler> handlersByType = new Dictionary<Type, IEventHandler>();

        public void Register(IEventHandler handler)
        {
            var genericHandler = typeof(IEventHandler<>);
            var supportedEventTypes = handler.GetType()
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericHandler)
                .Select(i => i.GetGenericArguments()[0])
                .ToList();

            if (this.handlersByType.Keys.Any(registeredType => supportedEventTypes.Contains(registeredType)))
            {
                var @event = this.handlersByType.Keys.First(registeredType => supportedEventTypes.Contains(registeredType));
                throw new ArgumentException(
                    $"The event {@event.Name} handled by the received handler {handler.GetType().Name} has a registered handler {this.handlersByType[@event].GetType().Name}.");

            }

            supportedEventTypes.ForEach(eventType => this.handlersByType.Add(eventType, handler));
        }

        public async Task Dispatch(IEvent @event)
        {
            var eventType = @event.GetType();

            IEventHandler handler;
            if (!this.handlersByType.TryGetValue(eventType, out handler))
                // Ty to invoke the generic handlers that have registered to handle IEvent directly, like the snapshotters.
                if (!this.handlersByType.TryGetValue(typeof(IEvent), out handler))
                {
                    throw new EventHandlerNotFoundException(eventType);
                }

            await ((dynamic)handler).Handle((dynamic)@event);
        }

        public IEnumerable<string> RegisteredEventTypes
        {
            get
            {
                var eventTypes = this.handlersByType.Keys.Select(k => k.Name.WithFirstCharInLower());

                if (eventTypes.Count() == 1 && eventTypes.First().ToLower() == "ievent")
                    return new string[0];

                return eventTypes;
            }
        }

        public void Dispose()
        {
            this.handlersByType.Values.ForEach(x =>
            {
                using (x as IDisposable)
                {
                    // Dispose handlers if applicable
                }
            });
        }

        public void NotifyLiveProcessingStarted()
        {
            this.handlersByType.Values.ForEach(x =>
            {
                x.NotifyLiveProcessingStarted();
            });
        }
    }
}
