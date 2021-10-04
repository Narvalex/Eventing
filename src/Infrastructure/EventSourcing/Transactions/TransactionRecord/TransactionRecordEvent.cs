using Infrastructure.Messaging;

namespace Infrastructure.EventSourcing.Transactions
{
    public abstract class TransactionRecordEvent : Event
    {
        public TransactionRecordEvent(string transactionId)
        {
            this.TransactionId = transactionId;
        }

        public override string StreamId => this.TransactionId;

        public string TransactionId { get; }
    }
}
