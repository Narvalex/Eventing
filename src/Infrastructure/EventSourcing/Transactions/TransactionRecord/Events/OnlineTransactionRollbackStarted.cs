namespace Infrastructure.EventSourcing.Transactions
{
    public class OnlineTransactionRollbackStarted : TransactionRecordEvent
    {
        public OnlineTransactionRollbackStarted(string transactionId) 
            : base(transactionId)
        {
        }

    }
}
