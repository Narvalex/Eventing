namespace Infrastructure.Messaging
{
    public interface ICommand : IMessage
    {
        string CommandId { get; }
        string CausationId { get; }
        string CorrelationId { get; }
    }
}
