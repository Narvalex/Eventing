namespace Infrastructure.EventSourcing.Transactions
{
    public class PreparedEventsBatchCleared : EntityTransactionPreparationEvent
    {
        public PreparedEventsBatchCleared(string entityStreamName, string transactionId, int batchSequenceNumber)
            : base(entityStreamName, transactionId)
        {
            this.BatchSequenceNumber = batchSequenceNumber;
        }

        public int BatchSequenceNumber { get; }
    }
}
