namespace Infrastructure.EventSourcing.Transactions
{
    public class OnlineTransactionRollbackCompleted : TransactionRecordEvent
    {
        public OnlineTransactionRollbackCompleted(string transactionId) 
            : base(transactionId)
        {
        }
    }
}
