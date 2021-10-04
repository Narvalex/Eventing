using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    /// <summary>
    /// Marker interface that makes it easier to discover event handlers via reflection.
    /// </summary>
    /// <remarks>
    /// The event handler implementation could be injected with a <see cref="System.Threading.CancellationToken"/> 
    /// that could be used to abort a long running event handling, that could be sleeping, for instance.
    /// </remarks>
    public interface IEventHandler 
    {
        /// <summary>
        /// Notify handlers that live processing has been started
        /// </summary>
        virtual void NotifyLiveProcessingStarted() { }
    }

    public interface IEventHandler<T> : IEventHandler where T : IEvent
    {
        Task Handle(T e);
    }
}
