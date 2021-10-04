namespace Infrastructure.EventSourcing.Transactions
{
    public class EventPrepared : EntityTransactionPreparationEvent
    {
        public EventPrepared(string entityStreamName, string transactionId, int batchSequenceNumber, long expectedVersion, string typeName, string payload)
            : base(entityStreamName, transactionId)
        {
            this.BatchSequenceNumber = batchSequenceNumber;
            this.ExpectedVersion = expectedVersion;
            this.TypeName = typeName;
            this.Payload = payload;
        }

        public int BatchSequenceNumber { get; }
        public long ExpectedVersion { get; }
        public string TypeName { get; }
        public string Payload { get; }
    }
}
