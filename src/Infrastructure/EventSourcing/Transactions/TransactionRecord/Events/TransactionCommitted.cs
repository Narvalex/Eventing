namespace Infrastructure.EventSourcing.Transactions
{
    public class TransactionCommitted : TransactionRecordEvent
    {
        public TransactionCommitted(string transactionId) 
            : base(transactionId)
        {
        }
    }
}
