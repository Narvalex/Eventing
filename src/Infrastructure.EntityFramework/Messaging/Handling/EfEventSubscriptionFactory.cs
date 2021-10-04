using Infrastructure.EntityFramework.EventStorage.Database;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.Messaging.Handling
{
    public class EfEventSubscriptionFactory : IEventSubscriptionFactory
    {
        private readonly Func<EventStoreDbContext> readContextFactory;
        private readonly EventDeserializationAndVersionManager serializer;
        private readonly TimeSpan pollDelay;

        public EfEventSubscriptionFactory(Func<EventStoreDbContext> readContextFactory, EventDeserializationAndVersionManager serializer, TimeSpan pollDelay)
        {
            this.readContextFactory = Ensured.NotNull(readContextFactory, nameof(readContextFactory));
            this.serializer = Ensured.NotNull(serializer, nameof(serializer));

            Ensure.NotNegative(pollDelay.TotalMilliseconds, nameof(pollDelay));
            this.pollDelay = pollDelay;
        }

        public Task<IEventSubscription> Create()
        {
            return Task.FromResult<IEventSubscription>(new EfEventSubscription(this.readContextFactory, this.serializer, this.pollDelay));
        }
    }
}
