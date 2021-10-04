using Infrastructure.EntityFramework.ReadModel;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Serialization;
using Infrastructure.Utils;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.EventLog
{
    public class EventLogProjection : BasicReadModelProjection<EventLogDbContext>,
        IEventHandler<IEvent>
    {
        private readonly IJsonSerializer serializer;

        public EventLogProjection(IEfReadModelProjector<EventLogDbContext> efReadModelProjector, IJsonSerializer serializer) 
            : base(efReadModelProjector)
        {
            this.serializer = serializer.EnsuredNotNull(nameof(serializer));
        }

        public Task Handle(IEvent e) =>
            this.Reduce(e, context =>
            {
                var metadata = e.GetEventMetadata();
                var chk = metadata.GetCheckpoint();

                context.Events.Add(new EventEntity
                {
                    EventNumber = metadata.EventNumber,
                    EventId = metadata.EventId,
                    CorrelationId = metadata.CorrelationId,
                    CausationId = metadata.CausationId,
                    CommitId = metadata.CommitId,
                    Timestamp = metadata.Timestamp,
                    EventSourcedType = metadata.EventSourcedType,
                    StreamId = e.StreamId,
                    EventSourcedVersion = metadata.EventSourcedVersion,
                    CausationNumber = metadata.CausationNumber,
                    EventType = metadata.EventType,
                    Payload = this.serializer.Serialize(e),
                    CommitPosition = chk.EventPosition.CommitPosition,
                    PreparePosition = chk.EventPosition.PreparePosition,
                    AuthorId = metadata.AuthorId,
                    AuthorName = metadata.AuthorName,
                    ClientIpAddress = metadata.ClientIpAddress,
                    UserAgent = metadata.UserAgent,
                    DisplayMode = metadata.DisplayMode,
                    CommandTimestamp = metadata.CommandTimestamp,
                    PositionLatitude = metadata.PositionLatitude,
                    PositionLongitude = metadata.PositionLongitude,
                    PositionAccuracy = metadata.PositionAccuracy,
                    PositionAltitude = metadata.PositionAltitude,
                    PositionAltitudeAccuracy = metadata.PositionAltitudeAccuracy,
                    PositionHeading = metadata.PositionHeading,
                    PositionSpeed = metadata.PositionSpeed,
                    PositionTimestamp = metadata.PositionTimestamp,
                    PositionError = metadata.PositionError
                });

                return Task.CompletedTask;
            });
    }
}
