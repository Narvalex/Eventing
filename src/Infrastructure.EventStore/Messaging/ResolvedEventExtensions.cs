using EventStore.ClientAPI;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using System.Text;

namespace Infrastructure.EventStore.Messaging
{
    public static class ResolvedEventExtensions
    {
        public static IEvent ToEventForHydration(this ResolvedEvent e, IEventDeserializationAndVersionManager versionManager)
            => MapToEventInterface(EventStream.NoEventsNumber, e, versionManager);

        public static IEvent ToEventForSubscription(this ResolvedEvent e, long eventNumber, IEventDeserializationAndVersionManager versionManager)
            => MapToEventInterface(eventNumber, e, versionManager);

        private static IEvent MapToEventInterface(long eventNumber, ResolvedEvent e, IEventDeserializationAndVersionManager versionManager)
            => versionManager
                .GetLatestEventVersion(
                    e.Event.EventType,
                    e.Event.EventNumber,
                    eventNumber,
                    Encoding.UTF8.GetString(e.Event.Data),
                    Encoding.UTF8.GetString(e.Event.Metadata),
                    EventStream.GetCategory(e.Event.EventStreamId));
    }
}
