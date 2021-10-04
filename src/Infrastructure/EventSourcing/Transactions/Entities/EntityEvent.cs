using Infrastructure.Messaging;

namespace Infrastructure.EventSourcing.Transactions
{
    public abstract class EntityEvent : Event
    {
        public EntityEvent(string streamId)
        {
            this.StreamId = streamId;
        }

        public override string StreamId { get; }
    }
}
