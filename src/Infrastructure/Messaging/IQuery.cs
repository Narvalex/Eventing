namespace Infrastructure.Messaging
{
    public interface IQuery : IMessage, IValidatable
    {
        string QueryId { get; }

        string GetCorrelationId();
    }
}
