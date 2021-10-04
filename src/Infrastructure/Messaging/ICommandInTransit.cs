namespace Infrastructure.Messaging
{
    public interface ICommandInTransit : ICommand
    {
        void SetMetadata(IMessageMetadata metadata);
        void SetCorrelationId(string correlationId);
        ICommandInTransit SetTransactionId(string transactionId);
        bool TryGetTransactionId(out string? transactionId);
    }
}
