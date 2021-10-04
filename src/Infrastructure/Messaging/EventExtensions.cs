using Infrastructure.EventSourcing;
using System;

namespace Infrastructure.Messaging
{
    public static class EventExtensions
    {
        public static string GetCorrelationId(this IEvent @event) => @event.GetEventMetadata().CorrelationId;

        public static string GetStreamIdToNotifyRejection(this IEvent incomingEvent)
            => $"rejection-{incomingEvent.GetCorrelationId()}";

        public static string GetStreamName(this IEvent @event) => EventStream.GetStreamName(@event);
    }
}
