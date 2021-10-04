namespace Infrastructure.EventSourcing.Transactions
{
    public class NewTransactionPrepareStarted : TransactionRecordEvent
    {
        public NewTransactionPrepareStarted(string transactionId) 
            : base(transactionId)
        {
        }
    }
}
