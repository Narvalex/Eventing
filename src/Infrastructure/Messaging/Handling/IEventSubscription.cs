using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    /// <summary>
    /// Abstracts the behavior of a receiver component that pushes an event for every received event.
    /// </summary>
    public interface IEventSubscription
    {
        /// <summary>
        /// Registers the action to be invoked whenever a new event apears.
        /// IMPORTANT: Subscription will drop ONLY after the handler is dropped. If handler is still running, it will
        /// not drop the subscription.
        /// </summary>
        Task StartAsync(
            string subscriptionName, 
            Checkpoint lastCheckpoint,
            Func<IEvent, Task> onEventAppeared,
            Action<string, Checkpoint> onEventIgnored,
            Action onLiveProcessingStarted, 
            Action<Exception, bool> onSubscriptionDropped,
            params string[] subscribedEventTypes);

        /// <summary>
        /// Tries to stop the sybscription in a non blocking way.
        /// </summary>
        Task Stop();
    }
}