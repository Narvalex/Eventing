namespace Infrastructure.EventSourcing.Transactions
{
    public class PreparedEventBatch : SubEntity<PreparedEventBatch>
    {
        public PreparedEventBatch(int id, long expectedVersion, StrSubEntities2<PreparedEventDescriptor> descriptors)
            : base(id)
        {
            this.ExpectedVersion = expectedVersion;
            this.Descriptors = descriptors;
        }

        public long ExpectedVersion { get; }
        public StrSubEntities2<PreparedEventDescriptor> Descriptors { get; }

        protected override void OnRegisteringHandlers(ISubEntityHandlerRegistry registry) { }
    }
}
