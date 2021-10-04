using Infrastructure.EntityFramework.ReadModel;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using System;
using System.Threading.Tasks;

namespace Erp.Domain.Tests.Helpers.ReadModelProjection
{
    public class ReadModelProjectionTesteable<TDbContext> where TDbContext : ReadModelDbContext
    {
        private readonly TestableReadModelProjection<TDbContext> sut;
        
        public ReadModelProjectionTesteable(params Func<IEfReadModelProjector<TDbContext>, BasicReadModelProjection<TDbContext>>[] projectionFactories)
        {
            EventSourced.SetValidNamespace("Erp.Domain");
            this.sut = new TestableReadModelProjection<TDbContext>(projectionFactories);
        }

        public ReadModelProjectionTesteable<TDbContext> Dado(params IEventInTransit[] history)
        {
            this.sut.Given(history);
            return this;
        }

        public ReadModelProjectionTesteable<TDbContext> Cuando(IEventInTransit @event)
        {
            this.sut.When(@event);
            return this;
        }

        public Task Entonces(Func<TDbContext, Task> aseveraciones)
        {
            return this.sut.Then(aseveraciones);
        }
    }
}
