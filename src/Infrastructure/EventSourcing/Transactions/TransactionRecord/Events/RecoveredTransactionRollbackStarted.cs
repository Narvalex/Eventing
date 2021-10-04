namespace Infrastructure.EventSourcing.Transactions
{
    public class RecoveredTransactionRollbackStarted : TransactionRecordEvent
    {
        public RecoveredTransactionRollbackStarted(string transactionId) 
            : base(transactionId)
        {
        }
    }
}
