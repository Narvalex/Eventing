namespace Infrastructure.Messaging
{
    public interface IEventUpcaster
    {
        string EventTypeToUpcast { get; }
        
        IEventInTransit Upcast(string payload, IEventMetadata metadata);
    }
}
