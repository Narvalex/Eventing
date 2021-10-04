namespace Infrastructure.EventSourcing.Transactions
{
    public enum TransactionStatus
    {
        NotStarted,
        PrepareStarted,
        CommitStarted,
        RollbackStarted,
        Closed
    }

    public enum TransactionOutcome
    {
        NotStarted,
        InProgress,
        Committed,
        Aborted
    }
}
