using Infrastructure.EntityFramework.ReadModel;
using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Erp.Domain.Tests.Helpers.ReadModelProjection
{
    public class BatchWriteTestReadModelProjector<T> : IEfReadModelProjector<T> where T : ReadModelDbContext
    {
        private readonly Queue<Tuple<IEvent, Func<T, Task>>> pendingWrites;
        private readonly IEventStore eventStore;

        public BatchWriteTestReadModelProjector(Queue<Tuple<IEvent, Func<T, Task>>> pendingWrites)
        {
            this.pendingWrites = pendingWrites;
            this.eventStore = new Mock<IEventStore>().Object;
        }

        public string ReadModelName => throw new NotImplementedException();

        public bool BatchWritesAreEnabled => throw new NotImplementedException();

        public Func<T> DbContextFactory => this.noopDbFactory;
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

        public Task Project(IEvent e, Func<T, Task> projection)
        {
            this.pendingWrites.Enqueue(new Tuple<IEvent, Func<T, Task>>(e, projection));
            return Task.CompletedTask;
        }

        public bool TryExtractCheckpoint(out Checkpoint checkpoint)
        {
            throw new NotImplementedException();
        }

        private T noopDbFactory()
        {
            return default(T)!;
        }
    }
}
