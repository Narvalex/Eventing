namespace Infrastructure.EventSourcing.Transactions
{
    public class TransactionCommitStarted : TransactionRecordEvent
    {
        public TransactionCommitStarted(string transactionId, TypeObject lastInteractedEntityType, string lastInteractedEntityId) 
            : base(transactionId)
        {
            this.LastInteractedEntityType = lastInteractedEntityType;
            this.LastInteractedEntityId = lastInteractedEntityId;
        }

        public TypeObject LastInteractedEntityType { get; }
        public string LastInteractedEntityId { get; }
    }
}
