using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EventStore.Tests.EventStorage
{
    public class FooEvent : TestEventBase
    {
        public FooEvent(string streamId) : base(streamId)
        {
        }
    }

    public class BarEvent : TestEventBase
    {
        public BarEvent(string streamId) : base(streamId)
        {
        }
    }

    public abstract class TestEventBase : IEventInTransit
    {
        private IEventMetadata metadata = MetadataHelper.NewEventMetadata();

        public TestEventBase(string streamId)
        {
            this.StreamId = streamId;
        }

        public string StreamId { get; }

        public bool InTransactionNow => false;

        public IEventMetadata GetEventMetadata() => this.metadata;

        public IMessageMetadata GetMessageMetadata() => this.metadata;

        public void SetEventMetadata(IEventMetadata metadata, string eventType) => this.metadata = metadata;

        public ValidationResult ExecuteBasicValidation() => ValidationResult.Ok();

        public Task ValidateEvent(IEventSourcedReader reader) => Task.CompletedTask;

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

    public static class MetadataHelper
    {
        public static EventMetadata NewEventMetadata() => new EventMetadata(Guid.NewGuid(),
            "corrId",
            "causId",
            "commitId",
            DateTime.Now,
                "author",
                "name",
                "ip",
                "user_agent");
    }
}
