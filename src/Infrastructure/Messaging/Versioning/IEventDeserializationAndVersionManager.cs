namespace Infrastructure.Messaging
{
    public interface IEventDeserializationAndVersionManager
    {
        IEvent GetLatestEventVersion(string eventType, long eventSourcedVersion, long eventNumber, string payload, string metadata, string eventSourcedType);
        IEvent GetLatestEventVersion(string eventType, long eventSourcedVersion, long eventNumber, string payload, IEventMetadata metadata, string eventSourcedType);
    }
}