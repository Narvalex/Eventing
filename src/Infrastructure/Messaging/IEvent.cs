namespace Infrastructure.Messaging
{
    public interface IEvent : IMessage
    {
        /// <summary>
        /// The entity/aggregate id, or the thing id, since it may not be an entity but more like a concept.
        /// </summary>
        string StreamId { get; }

        // Set as a method to avoid serialization
        IEventMetadata GetEventMetadata();
    }
}
