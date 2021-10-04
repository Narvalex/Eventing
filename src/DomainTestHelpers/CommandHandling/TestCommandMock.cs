using Infrastructure.Messaging;

namespace Erp.Domain.Tests.Helpers
{
    public class TestCommandMock : Command
    {
        public TestCommandMock(string correlationId = null)
        {
            this.CorrelationId = correlationId is null ? this.CommandId : correlationId;
            this.metadata = new MessageMetadata("admin", "admin", "localhost", "visual studio");
        }

        public override string CorrelationId { get; protected set; }
    }

    public class TestEventMock : Event
    {
        public TestEventMock(string streamId)
        {
            this.StreamId = streamId;
        }

        public override string StreamId { get; }
    }
}
