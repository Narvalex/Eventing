using EventStore.ClientAPI;
using Infrastructure.Messaging.Handling;

namespace Infrastructure.EventStore.Messaging.Handling
{
    public static class CheckpointExtensions
    {
        public static Position ToEventStorePosition(this Checkpoint checkpoint) =>
            new Position(checkpoint.EventPosition.CommitPosition, checkpoint.EventPosition.PreparePosition);

    }
}
