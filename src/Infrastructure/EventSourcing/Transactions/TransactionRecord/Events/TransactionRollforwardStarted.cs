namespace Infrastructure.EventSourcing.Transactions
{
    public class TransactionRollforwardStarted : TransactionRecordEvent
    {
        public TransactionRollforwardStarted(string transactionId) 
            : base(transactionId)
        {
        }
    }
}
