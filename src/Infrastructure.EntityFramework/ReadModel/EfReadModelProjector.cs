using Infrastructure.DateTimeProvider;
using Infrastructure.EventSourcing;
using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.ReadModel
{
    public class EfReadModelProjector<T> : EfReadModelProjectorBase, IEfReadModelProjector<T> where T : ReadModelDbContext
    {
        private readonly Func<T> contextFactory;
        protected readonly IDateTimeProvider dateTime;

        public EfReadModelProjector(Func<T> contextFactory, IEventStore eventStore, IDateTimeProvider dateTime)
        {
            this.contextFactory = Ensured.NotNull(contextFactory, nameof(contextFactory));
            this.ReadModelName = ReadModelDbContext.ResolveReadModelName<T>();
            this.dateTime = Ensured.NotNull(dateTime, nameof(dateTime));
            this.EventStore = eventStore.EnsuredNotNull(nameof(eventStore));
        }

        public string ReadModelName { get; }

        public virtual bool BatchWritesAreEnabled => false;

        public Func<T> DbContextFactory => this.contextFactory;

        public IEventStore EventStore { get; }

        public virtual bool IsBatchWritingNow => false;

        public virtual void LiveProcessingStarted()
        {
        }

        // TODO: This could be used to batch writes in a "Direct write" read model
        // In EventProcessor y should be set to always use the Checkpoint provider
        // and then here batch all writes
        // This could make hard to test individual events projection
        public virtual async Task Project(IEvent e, Func<T, Task> projection)
        {
            var checkpointFromEvent = e.GetEventMetadata().GetCheckpoint();

            var cacheNeedsToBeSet = false;
            if (this.LastEventNumber != EventStream.NoEventsNumber)
            {
                if (this.LastEventNumber >= checkpointFromEvent.EventNumber)
                    return; // fast idempotence, not even a connection was opened :D
            }
            else
                cacheNeedsToBeSet = true;

            using (var context = contextFactory.Invoke())
            {
                var checkpointEntity = context.Checkpoints.FirstOrDefault(x => x.Id == ReadModelCheckpointEntity.IDEMPOTENCY_CHK);

                if (checkpointEntity != null)
                {
                    var checkpointFromDb = new Checkpoint(new EventPosition(checkpointEntity.CommitPosition, checkpointEntity.PreparePosition), checkpointEntity.EventNumber);
                    if (cacheNeedsToBeSet)
                        this.PrepareCheckpoint(checkpointFromDb);

                    if (checkpointFromDb.EventNumber >= checkpointFromEvent.EventNumber)
                        return; // idempotence with possible cache update

                    checkpointEntity.EventNumber = checkpointFromEvent.EventNumber;
                    checkpointEntity.PreparePosition = checkpointFromEvent.EventPosition.PreparePosition;
                    checkpointEntity.CommitPosition = checkpointFromEvent.EventPosition.CommitPosition;
                    checkpointEntity.TimeStamp = this.dateTime.Now;
                }
                else
                {
                    context.Checkpoints.Add(new ReadModelCheckpointEntity
                    {
                        Id = ReadModelCheckpointEntity.IDEMPOTENCY_CHK,
                        EventNumber = checkpointFromEvent.EventNumber,
                        PreparePosition = checkpointFromEvent.EventPosition.PreparePosition,
                        CommitPosition = checkpointFromEvent.EventPosition.CommitPosition,
                        TimeStamp = this.dateTime.Now
                    });
                }

                await projection(context);

                await context.SafeSaveChangesAsync();

                this.SetCheckpointForExtraction(checkpointFromEvent);
            }
        }

        public virtual void OnBatchCommited(Action<Checkpoint> checkpoint) { }
    }
}
