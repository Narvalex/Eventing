using Infrastructure.Messaging;

namespace Infrastructure.EventSourcing
{
    public class UpdateEventSourcedParams
    {
        public UpdateEventSourcedParams(string correlationId, string causationId, long? causationNumber, IMessageMetadata metadata, bool isCommandMetadata)
        {
            this.CorrelationId = correlationId;
            this.CausationId = causationId;
            this.CausationNumber = causationNumber;
            this.Metadata = metadata;
            this.IsCommandMetadata = isCommandMetadata;
        }

        public string CorrelationId { get; }
        public string CausationId { get; }
        public long? CausationNumber { get; }
        public IMessageMetadata Metadata { get; }
        public bool IsCommandMetadata { get; }
    }
}
