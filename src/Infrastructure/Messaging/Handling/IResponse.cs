namespace Infrastructure.Messaging.Handling
{
    public interface IResponse<T> : IHandlingResult
    {
        T Payload { get; }
    }
}
