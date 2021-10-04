namespace Infrastructure.EventSourcing.Transactions
{
    public class LockAcquired : EntityEvent
    {
        public LockAcquired(string streamId, string lockOwnerId)
            : base(streamId)
        {
            this.LockOwnerId = lockOwnerId;
        }

        public string LockOwnerId { get; }
    }
}
