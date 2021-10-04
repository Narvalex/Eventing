namespace Infrastructure.EventSourcing.Transactions
{
    public class EntityTransactionPreparationCreated : EntityTransactionPreparationEvent
    {
        public EntityTransactionPreparationCreated(string entityStreamName, string transactionId, TypeObject entityType)
            : base(entityStreamName, transactionId)
        {
            this.EntityType = entityType;
        }

        public TypeObject EntityType { get; }
    }
}
