using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface IEventDispatcher : IDisposable // Explicitly to dispose batch processors
    {
        void Register(IEventHandler handler);

        Task Dispatch(IEvent @event);

        IEnumerable<string> RegisteredEventTypes { get; }

        void NotifyLiveProcessingStarted();
    }
}
