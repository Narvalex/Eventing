using Infrastructure.Messaging;

namespace Infrastructure.EventStorage
{
    /// <summary>
    /// A stream events slice represents the result of a single read operation to <see cref="IEventStore"/>.
    /// </summary>
    public class EventStreamSlice : StreamReadSlice
    {
        public EventStreamSlice(SliceFetchStatus status, IEvent[] events, long nextEventNumber, bool IsEndOfStream)
            : base(status, nextEventNumber, IsEndOfStream)
        {
            this.Events = events;
        }

        /// <summary>
        /// The events read represented as <see cref="IEvent"/>.
        /// </summary>
        public IEvent[] Events { get; }
    }
}
