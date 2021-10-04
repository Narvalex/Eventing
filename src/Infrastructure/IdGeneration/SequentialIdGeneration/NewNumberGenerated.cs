using Infrastructure.Messaging;

namespace Infrastructure.IdGeneration
{
    public class NewNumberGenerated : Event
    {
        public NewNumberGenerated(string streamId)
        {
            this.StreamId = streamId;
        }

        public override string StreamId { get; }
    }
}
