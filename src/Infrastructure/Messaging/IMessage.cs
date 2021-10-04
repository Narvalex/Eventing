namespace Infrastructure.Messaging
{
    /// <summary>
    /// The messages could be anything, but for subscriptions, it should be a message with a metadata.
    /// It's being used methods to avoid being serialized.
    /// </summary>
    public interface IMessage : IValidatable
    {
        // Set as a method to avoid serialization
        IMessageMetadata GetMessageMetadata();
    }
}
