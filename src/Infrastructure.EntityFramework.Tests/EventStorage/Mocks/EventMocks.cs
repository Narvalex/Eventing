using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.Tests.EventStorage.Helpers
{
    public class FooEvent : TestEventBase
    {
        public FooEvent(string streamId, string category = "tests")
            : base(streamId, category)
        {
        }
    }

    public class BarEvent : TestEventBase
    {
        public BarEvent(string streamId, string category = "tests")
            : base(streamId, category)
        {
        }
    }

    public abstract class TestEventBase : IEventInTransit
    {
        private IEventMetadata metadata;

        public TestEventBase(string sourceId, string category)
        {
            this.StreamId = sourceId;
            this.Category = category;
            this.metadata = MetadataHelper.NewEventMetadata();
        }

        public string StreamId { get; }
        public string Category { get; }

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
