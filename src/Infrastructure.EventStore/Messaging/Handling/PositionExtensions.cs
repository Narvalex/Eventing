using EventStore.ClientAPI;
using Infrastructure.Messaging.Handling;

namespace Infrastructure.EventStore.Messaging.Handling
{
    public static class PositionExtensions
    {
        public static EventPosition ToEventPosition(this Position position) =>
            new EventPosition(position.CommitPosition, position.PreparePosition);
    }
}
