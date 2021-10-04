using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using System.Collections.Generic;

namespace Infrastructure.Tests.EventSourcing
{
    public class StatelessTestEntity : EventSourced
    {
        public StatelessTestEntity(EventSourcedMetadata metadata) : base(metadata)
        {
        }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry)
        {
        }
    }

    public class LogEvent : StatelessTestEntityEventBase
    {
        public LogEvent(string sourceId, string foo)
            : base(sourceId)
        {
            this.Foo = foo;
        }

        public string Foo { get; }
    }

    // Good practice
    public class StatelessTestEntityEventBase : Event
    {
        public StatelessTestEntityEventBase(string sourceId)
        {
            this.StreamId = sourceId;
        }

        public override string StreamId { get; }
    }
}
