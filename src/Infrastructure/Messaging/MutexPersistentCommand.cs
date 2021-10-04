namespace Infrastructure.Messaging
{
    public abstract class MutexPersistentCommand : Event
    {
        protected MutexPersistentCommand(string mutexId)
        {
            this.MutexId = mutexId;
        }

        public override string StreamId => this.MutexId;

        public string MutexId { get; }
    }
}
