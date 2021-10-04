namespace Infrastructure.Messaging.Handling
{
    public interface IReadModelProjection : IReadModelProjectionCheckpointProvider, IEventHandler
    {
        string ReadModelName { get; }
    }
}
