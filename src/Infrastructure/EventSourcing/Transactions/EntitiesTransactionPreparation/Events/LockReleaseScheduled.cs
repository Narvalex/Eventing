namespace Infrastructure.EventSourcing.Transactions
{
    public class LockReleaseScheduled : EntityTransactionPreparationEvent
    {
        public LockReleaseScheduled(string entityStreamName, string transactionId)
            : base(entityStreamName, transactionId)
        {
        }
    }
}
