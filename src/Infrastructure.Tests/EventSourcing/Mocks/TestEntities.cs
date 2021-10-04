using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using System.Collections.Generic;

namespace Infrastructure.Tests.EventSourcing
{
    public class TestEntities : EventSourced
    {
        public TestEntities(EventSourcedMetadata metadata, bool deleted) : base(metadata)
        {
            this.Deleted = deleted;
        }

        public bool Deleted { get; private set; } = false;

        protected override void OnOutputState()
        {
            this.Deleted = !(!this.Deleted);
        }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry)
        {
            registry
            .On<TestEvent>()
            .On<TestEventWithMetadata>(x =>
            {
                this.Deleted = true;
            });
        }
    }

    public class TestEventWithMetadata : IEvent
    {
        private readonly IEventMetadata metadata;

        public TestEventWithMetadata(string sourceId, string foo, IEventMetadata metadata)
        {
            this.StreamId = sourceId;
            this.Foo = foo;
            this.metadata = metadata;
        }

        public string Foo { get; }

        public string StreamId { get; }

        public ValidationResult ExecuteBasicValidation() => ValidationResult.Ok();

        public IEventMetadata GetEventMetadata() => this.metadata;

        public IMessageMetadata GetMessageMetadata() => this.metadata;
    }

    public class TestEvent : TestEntityEventBase
    {
        public TestEvent(string sourceId, string foo)
            :base(sourceId)
        {
            this.Foo = foo;
        }

        public string Foo { get; }
    }

    // Good practice
    public class TestEntityEventBase : Event
    {
        public TestEntityEventBase(string sourceId)
        {
            this.StreamId = sourceId;
        }

        public override string StreamId { get; }
    }
}
