using System;

namespace Infrastructure.Messaging.Handling
{
    public class EventHandlerNotFoundException : Exception
    {
        public EventHandlerNotFoundException(Type eventType)
            : base($"The handler for event {eventType.Name} was not found.")
        {
        }
    }
}
