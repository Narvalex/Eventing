using Infrastructure.Messaging;
using System;

namespace Infrastructure.Tests.Messaging.Handling.Mocks
{
    public class FooEvent : IEvent
    {
        private readonly IEventMetadata metadata;

        public FooEvent(IEventMetadata metadata = null)
        {
            this.metadata = metadata;
        }

        public string StreamId => throw new NotImplementedException();

        public IEventMetadata GetEventMetadata() => this.metadata;

        public IMessageMetadata GetMessageMetadata() => this.metadata;

        public ValidationResult ExecuteBasicValidation() => ValidationResult.Ok();
    }
    public class BarEvent : IEvent
    {
        private readonly IEventMetadata metadata;

        public BarEvent(IEventMetadata metadata = null)
        {
            this.metadata = metadata;
        }

        public string StreamId => throw new NotImplementedException();

        public IEventMetadata GetEventMetadata() => this.metadata;

        public IMessageMetadata GetMessageMetadata() => this.metadata;

        public ValidationResult ExecuteBasicValidation() => ValidationResult.Ok();
    }
}
