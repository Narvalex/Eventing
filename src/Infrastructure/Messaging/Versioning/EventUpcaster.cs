using Infrastructure.EventSourcing;
using Infrastructure.Serialization;
using Infrastructure.Utils;

namespace Infrastructure.Messaging.Versioning
{
    public abstract class EventUpcaster : IEventUpcaster
    {
        protected readonly IJsonSerializer serializer;
        protected readonly IEventSourcedReader reader;

        public EventUpcaster(IJsonSerializer serializer)
        {
            this.serializer = Ensured.NotNull(serializer, nameof(serializer));
        }

        public EventUpcaster(IJsonSerializer serializer, IEventSourcedReader reader)
            : this(serializer)
        {
            this.reader = Ensured.NotNull(reader, nameof(reader));
        }

        /// <summary>
        /// The event type to upcast, in cammel case. Eg. 'newContactCreated'
        /// </summary>
        public abstract string EventTypeToUpcast { get; }

        public abstract IEventInTransit Upcast(string payload, IEventMetadata metadata);
    }
}
