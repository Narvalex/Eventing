namespace Infrastructure.EventSourcing.Transactions
{
    public class LockReleased : EntityEvent
    {
        public LockReleased(string streamId, string formerLockOwnerId)
            : base(streamId)
        {
            this.FormerLockOwnerId = formerLockOwnerId;
        }

        public string FormerLockOwnerId { get; }
    }
}
