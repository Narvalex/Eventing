using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.Tests.Messaging.Handling.Mocks
{
    public class FooEvent : TestEventBase
    {
        public FooEvent(string sourceId) : base(sourceId)
        {
        }
    }

    public class BarEvent : TestEventBase
    {
        public BarEvent(string sourceId) : base(sourceId)
        {
        }
    }

    public abstract class TestEventBase : IEventInTransit
    {
        private IEventMetadata metadata = MetadataHelper.NewEventMetadata();

        public TestEventBase(string sourceId)
        {
            this.StreamId = sourceId;
        }

        public string StreamId { get; }

        public bool InTransactionNow => false;

        public Task ValidateEvent(IEventSourcedReader reader) => Task.CompletedTask;

        public ValidationResult ExecuteBasicValidation() => ValidationResult.Ok();

        public IEventMetadata GetEventMetadata() => this.metadata;

        public IMessageMetadata GetMessageMetadata() => this.metadata;

        public void SetEventMetadata(IEventMetadata metadata, string eventType) => this.metadata = metadata;

        public IEventInTransit SetTransactionId(string transactionId)
        {
            return this;
        }

        public bool CheckIfEventBelongsToTransaction(string transactionId)
        {
            return false;
        }

        public bool TryGetTransactionId(out string? transactionId)
        {
            transactionId = null;
            return false;
        }
    }
}
