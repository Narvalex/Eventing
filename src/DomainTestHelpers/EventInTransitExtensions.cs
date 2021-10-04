using Infrastructure.Messaging;
using Infrastructure.Utils;
using System;

namespace Erp.Domain.Tests.Helpers
{
    public static class EventInTransitExtensions
    {
        public static IEventInTransit WithCorrelationId(this IEventInTransit @event, string correlationId)
        {
            var metadata = @event.GetEventMetadata();
            return ResolveEventWithCustomMetadata(@event, metadata?.EventId, correlationId, metadata?.CausationId, metadata?.Timestamp, metadata?.CausationNumber, metadata?.EventNumber);
        }

        public static IEventInTransit WithCausationId(this IEventInTransit @event, string causationId)
        {
            var metadata = @event.GetEventMetadata();
            return ResolveEventWithCustomMetadata(@event, metadata?.EventId, metadata?.CorrelationId, causationId, metadata?.Timestamp, metadata?.CausationNumber, metadata?.EventNumber);
        }

        public static IEventInTransit WithId(this IEventInTransit @event, Guid id)
        {
            var metadata = @event.GetEventMetadata();
            return ResolveEventWithCustomMetadata(@event, id, metadata?.CorrelationId, metadata?.CausationId, metadata?.Timestamp, metadata?.CausationNumber, metadata?.EventNumber);
        }

        public static IEventInTransit WithTimestamp(this IEventInTransit @event, DateTime timestamp)
        {
            var metadata = @event.GetEventMetadata();
            return ResolveEventWithCustomMetadata(@event, metadata?.EventId, metadata?.CorrelationId, metadata?.CausationId, timestamp, metadata?.CausationNumber, metadata?.EventNumber);
        }

        public static IEventInTransit WithCausationNumber(this IEventInTransit @event, long causationNumber)
        {
            var metadata = @event.GetEventMetadata();
            return ResolveEventWithCustomMetadata(@event, metadata?.EventId, metadata?.CorrelationId, metadata?.CausationId, metadata?.Timestamp, causationNumber, metadata?.EventNumber);
        }

        public static IEventInTransit WithEventNumber(this IEventInTransit @event, long eventNumber)
        {
            var metadata = @event.GetEventMetadata();
            return ResolveEventWithCustomMetadata(@event, metadata?.EventId, metadata?.CorrelationId, metadata?.CausationId, metadata?.Timestamp, metadata?.CausationNumber, eventNumber);
        }

        private static IEventInTransit ResolveEventWithCustomMetadata(IEventInTransit @event, Guid? eventId, string? correlationId, string? causationId, DateTime? timeStamp, long? causationNumber, long? eventNumber) // do not put optional parameters. We need all args to chain param adding.
        {
            var metadata = new EventMetadata(
                eventId.HasValue ? eventId.Value : Guid.NewGuid(),
                correlationId ?? Guid.NewGuid().ToString(),
                causationId ?? Guid.NewGuid().ToString(),
                "commitId",
                timeStamp ?? DateTime.Now,
                 "author",
                 "name",
                 "ip",
                 "user_agent",
                 causationNumber
            );

            metadata.SetOfEventNumberForTestOnly(eventNumber ?? 0);

            @event.SetEventMetadata(metadata, @event.GetType().Name.WithFirstCharInLower());

            return @event;
        }
    }
}
