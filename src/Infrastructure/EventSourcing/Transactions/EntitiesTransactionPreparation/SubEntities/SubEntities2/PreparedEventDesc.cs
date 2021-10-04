namespace Infrastructure.EventSourcing.Transactions
{
    public class PreparedEventDescriptor : StrSubEntity2<PreparedEventDescriptor>
    {
        public PreparedEventDescriptor(string id, string typeName, string payload)
            : base(id)
        {
            this.TypeName = typeName;
            this.Payload = payload;
        }

        public string TypeName { get; }
        public string Payload { get; }

        protected override void OnRegisteringHandlers(IStrSubEntity2HandlerRegistry registry) { }
    }
}
