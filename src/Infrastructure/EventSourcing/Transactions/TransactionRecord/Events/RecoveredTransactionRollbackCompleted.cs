namespace Infrastructure.EventSourcing.Transactions
{
    public class RecoveredTransactionRollbackCompleted : TransactionRecordEvent
    {
        public RecoveredTransactionRollbackCompleted(string transactionId) : base(transactionId)
        {
        }
    }
}
