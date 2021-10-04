using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    // Based on: https://github.com/microsoftarchive/cqrs-journey/blob/master/source/Infrastructure/Infrastructure/Messaging/Handling/EventDispatcher.cs
    // And inspired from: https://stackoverflow.com/questions/7478387/how-does-having-a-dynamic-variable-affect-performance
    public class PrecompiledEventDispatcher : IEventDispatcher
    {
        private readonly Dictionary<Type, Tuple<string, Func<IEvent, Task>>> handlersByEventType = new Dictionary<Type, Tuple<string, Func<IEvent, Task>>>();
        private readonly List<IEventHandler> eventHandlers = new List<IEventHandler>();

        public void Register(IEventHandler handler)
        {
            handler
               .GetType()
               .GetInterfaces()
               .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
               .Select(i => new
               {
                   HandlerInterface = i,
                   EventType = i.GetGenericArguments()[0]
               })
               .Select(c => new Tuple<Type, string, Func<IEvent, Task>>(
                   c.EventType,
                   handler.ToString(),
                   c.Transform(x =>
                   {
                       var parameter = Expression.Parameter(typeof(IEvent));
                       var invocationExpression =
                           Expression.Lambda(
                               Expression.Block(
                                   Expression.Call(
                                       Expression.Convert(Expression.Constant(handler), x.HandlerInterface),
                                       x.HandlerInterface.GetMethod("Handle"),
                                       Expression.Convert(parameter, x.EventType))
                                   ),
                               parameter);

                       return (Func<IEvent, Task>)invocationExpression.Compile();
                   })))
               .ForEach(x =>
               {
                   if (this.handlersByEventType.ContainsKey(x.Item1))
                       throw new ArgumentException(
                           $"The command {x.Item1.Name} hanled by the received handler {handler.GetType().Name} has a registered handler {this.handlersByEventType[x.Item1].Item1}.");

                   this.handlersByEventType.Add(x.Item1, new Tuple<string, Func<IEvent, Task>>(x.Item2, x.Item3));
               });

            this.eventHandlers.Add(handler);
        }

        public async Task Dispatch(IEvent @event)
        {
            var eventType = @event.GetType();

            if (!this.handlersByEventType.TryGetValue(eventType, out var handler))
                // Ty to invoke the generic handlers that have registered to handle IEvent directly, like the snapshotters.
                if (!this.handlersByEventType.TryGetValue(typeof(IEvent), out handler))
                {
                    throw new EventHandlerNotFoundException(eventType);
                }

            await handler.Item2.Invoke(@event);
        }

        public IEnumerable<string> RegisteredEventTypes
        {
            get
            {
                var eventTypes = this.handlersByEventType.Keys.Select(k => k.Name.WithFirstCharInLower());

                if (eventTypes.Count() == 1 && eventTypes.First().ToLower() == "ievent")
                    return new string[0];

                return eventTypes;
            }
        }

        public void Dispose()
        {
            this.eventHandlers.ForEach(x =>
            {
                using (x as IDisposable)
                {
                    // Dispose handlers if applicable
                }
            });
        }

        public void NotifyLiveProcessingStarted()
        {
            this.eventHandlers.ForEach(x => x.NotifyLiveProcessingStarted());
        }
    }
}
