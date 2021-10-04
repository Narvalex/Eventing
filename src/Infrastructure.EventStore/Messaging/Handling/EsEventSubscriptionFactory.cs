using EventStore.ClientAPI;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EventStore.Messaging.Handling
{
    public class EsEventSubscriptionFactory : IEventSubscriptionFactory
    {
        private readonly Func<Task<IEventStoreConnection>> resilientConnectionFactory;
        private readonly EventDeserializationAndVersionManager versionManager;

        public EsEventSubscriptionFactory(Func<Task<IEventStoreConnection>> resilientConnectionFactory, EventDeserializationAndVersionManager versionManager)
        {
            this.resilientConnectionFactory = Ensured.NotNull(resilientConnectionFactory, nameof(resilientConnectionFactory));
            this.versionManager = Ensured.NotNull(versionManager, nameof(versionManager));
        }

        public async Task<IEventSubscription> Create()
        {
            return new EsEventSubscription(await this.resilientConnectionFactory.Invoke(), versionManager);
        }
    }
}
