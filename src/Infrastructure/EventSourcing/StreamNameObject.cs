using Infrastructure.Messaging;
using Infrastructure.Utils;

namespace Infrastructure.EventSourcing
{
    internal class StreamNameObject
    {
        private bool nameHasBeenSet = false;
        private string _streamId;

        public StreamNameObject(IEventSourced eventSourced, string streamName = null)
        {
            if (streamName.IsEmpty())
                this.Category = EventStream.GetCategory(eventSourced.GetType());
            else
            {
                this.Name = streamName;
                this.StreamId = EventStream.GetId(streamName);
                this.Category = EventStream.GetCategory(streamName);
                this.MarkAsSet();
            }
        }

        internal string Name { get; private set; }
        internal string StreamId { get => _streamId; private set => _streamId = Ensured.NotEmpty(value, "The streamId can not be empty"); }
        internal string Category { get; private set; }

        internal void SetNameIfNeeded(IEvent @event)
        {
            if (this.nameHasBeenSet)
                return;

            this.StreamId = @event.StreamId;
            this.Name = EventStream.GetStreamName(this.Category, this.StreamId);
            this.MarkAsSet();
        }

        private void MarkAsSet()
        {
            this.nameHasBeenSet = true;
        }
    }
}
