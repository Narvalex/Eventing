namespace Infrastructure.Messaging.Handling
{
    public interface IHandlingResult
    {
        bool Success { get; }
        string[] Messages { get; }
    }
}
