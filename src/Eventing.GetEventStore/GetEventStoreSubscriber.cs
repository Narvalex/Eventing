using Eventing.Core.Persistence;
using Eventing.Core.Serialization;
using EventStore.ClientAPI;
using System;

namespace Eventing.GetEventStore
{
    public class GetEventStoreSubscriber : IEventSubscriber
    {
        private readonly IEventStoreConnection resilientConnection;
        private readonly IJsonSerializer serializer;

        public GetEventStoreSubscriber(IEventStoreConnection resilientConnection, IJsonSerializer serializer)
        {
            Ensure.NotNull(resilientConnection, nameof(resilientConnection));
            Ensure.NotNull(serializer, nameof(serializer));

            this.resilientConnection = resilientConnection;
            this.serializer = serializer;
        }

        public IEventSubscription CreateSubscription(string streamName, Lazy<long?> lastCheckpoint, Action<long, object> handler)
        {
            return new GetEventStoreSubscription(
                this.resilientConnection,
                this.serializer,
                streamName,
                lastCheckpoint,
                handler);
        }
    }
}

