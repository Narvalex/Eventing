namespace Infrastructure.EventSourcing
{
    public class EventSourcedMetadata 
    {
        public EventSourcedMetadata(string streamName, long version, long lastCausationNumber, bool exists, bool isLocked, string? lockOwnerId)
        {
            this.StreamName = streamName;
            this.Version = version;
            this.LastCausationNumber = lastCausationNumber;
            this.Exists = exists;
            this.IsLocked = isLocked;
            this.LockOwnerId = lockOwnerId;
        }

        public string StreamName { get; }
        public long Version { get; }
        public long LastCausationNumber { get; }
        public bool Exists { get; }
        public bool IsLocked { get; }
        public string? LockOwnerId { get; }
    }
}
