using Infrastructure.Messaging;

namespace Infrastructure.EventSourcing.Transactions
{
    public abstract class EntityTransactionPreparationEvent : Event
    {
        public EntityTransactionPreparationEvent(string entityStreamName, string transactionId)
        {
            this.EntityStreamName = entityStreamName;
            this.TransactionId = transactionId;
        }

        public string EntityStreamName { get; }
        public string TransactionId { get; }

        public override string StreamId => this.EntityStreamName;
    }
}
