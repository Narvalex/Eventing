using Infrastructure.DateTimeProvider;
using Infrastructure.EntityFramework;
using Infrastructure.EntityFramework.ReadModel;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Erp.Domain.Tests.Helpers.ReadModelProjection
{
    public class TestableReadModelProjection<TDbContext> where TDbContext : ReadModelDbContext
    {
        // SET DIFERENT DATABASES HERE

        // Direct
        private EfDbInitializer<TDbContext> directContextResolver;
        private DynamicEventDispatcher directDispatcher = new DynamicEventDispatcher();
        // Batch
        private readonly Queue<Tuple<IEvent, Func<TDbContext, Task>>> pendingWrites = new Queue<Tuple<IEvent, Func<TDbContext, Task>>>();
        private EfDbInitializer<TDbContext> batchContextResolver;
        private DynamicEventDispatcher batchDispatcher = new DynamicEventDispatcher();

        private List<IEventInTransit> events = new List<IEventInTransit>();

        public TestableReadModelProjection(params Func<IEfReadModelProjector<TDbContext>, BasicReadModelProjection<TDbContext>>[] projectionFactories)
        {
            // Direct
            this.directContextResolver = new EfDbInitializer<TDbContext>();
            foreach (var factory in projectionFactories)
            {
                var projection = factory.Invoke(new DirectWriteTestReadModelProjector<TDbContext>(directContextResolver.ResolveWriteContext));
                this.directDispatcher.Register(projection);
            }
            // Batch
            this.batchContextResolver = new EfDbInitializer<TDbContext>();
            foreach (var factory in projectionFactories)
            {
                var projection = factory.Invoke(new BatchWriteTestReadModelProjector<TDbContext>(this.pendingWrites));
                this.batchDispatcher.Register(projection);
            }
        }

        public TestableReadModelProjection<TDbContext> Given(params IEventInTransit[] history)
        {
            this.events.AddRange(this.SetMetadataWhenNeeded(history));
            return this;
        }

        public TestableReadModelProjection<TDbContext> When(IEventInTransit @event)
        {
            this.events.Add(this.SetMetadataWhenNeeded(@event));
            return this;
        }

        public async Task Then(Func<TDbContext, Task> assertions)
        {
            // Direct writes
            foreach (var e in this.events)
                await this.directDispatcher.Dispatch(e);

            using (var context = this.directContextResolver.ResolveReadContext())
            {
                await assertions.Invoke(context);
            }

            // Batch writes. Nothing is written here, only enqueued
            foreach (var e in this.events)
                await this.batchDispatcher.Dispatch(e);

            using (var context = this.batchContextResolver.ResolveWriteContext())
            {
                while (this.pendingWrites.TryDequeue(out var tuple))
                {
                    var e = tuple.Item1;
                    var projection = tuple.Item2;

                    await projection(context);

                };

                await context.UnsafeSaveChangesAsync();
            }

            using (var context = this.batchContextResolver.ResolveReadContext())
            {
                await assertions.Invoke(context);
            }
        }

        private IEnumerable<IEventInTransit> SetMetadataWhenNeeded(params IEventInTransit[] history)
        {
            return history.Select(x => this.SetMetadataWhenNeeded(x));
        }


        private IEventInTransit SetMetadataWhenNeeded(IEventInTransit e)
        {
            if (e.GetEventMetadata() != null)
                return e;

            // No tiene ningun solo metadato
            var metadata = new EventMetadata(
                Guid.NewGuid(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                "commitId",
                DateTime.Now,
                 "author",
                 "name",
                 "ip",
                 "user_agent"
                );


           e.SetEventMetadata(metadata, e.GetType().Name.WithFirstCharInLower());

            return e;
        }
    }
}
