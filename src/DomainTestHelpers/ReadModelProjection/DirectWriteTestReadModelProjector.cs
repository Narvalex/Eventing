using Infrastructure.EntityFramework.ReadModel;
using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Moq;
using System;
using System.Threading.Tasks;

namespace Erp.Domain.Tests.Helpers.ReadModelProjection
{
    public class DirectWriteTestReadModelProjector<T> : IEfReadModelProjector<T> where T : ReadModelDbContext
    {
        private readonly Func<T> contextFactory;
        private readonly IEventStore eventStore;

        public DirectWriteTestReadModelProjector(Func<T> contextFactory)
        {
            this.contextFactory = contextFactory;
            this.eventStore = new Mock<IEventStore>().Object;
        }

        public string ReadModelName => throw new NotImplementedException();

        public bool BatchWritesAreEnabled => throw new NotImplementedException();

        public Func<T> DbContextFactory => this.contextFactory;

        public IEventStore EventStore => this.eventStore;

        public bool IsBatchWritingNow => throw new NotImplementedException();

        public void LiveProcessingStarted()
        {
            throw new NotImplementedException();
        }

        public void OnBatchCommited(Action<Checkpoint> checkpoint)
        {
            throw new NotImplementedException();
        }

        public async Task Project(IEvent e, Func<T, Task> projection)
        {
            using (var context = this.contextFactory())
            {
                await projection(context);

                await context.UnsafeSaveChangesAsync();
            }
        }

        public bool TryExtractCheckpoint(out Checkpoint checkpoint)
        {
            throw new NotImplementedException();
        }
    }
}
