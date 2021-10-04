namespace Infrastructure.EventSourcing.Transactions
{
    public class TransactionRollforwardCompleted : TransactionRecordEvent
    {
        public TransactionRollforwardCompleted(string transactionId) 
            : base(transactionId)
        {
        }
    }
}
