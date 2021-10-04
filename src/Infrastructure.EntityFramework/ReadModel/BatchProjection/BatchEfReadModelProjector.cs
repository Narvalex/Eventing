using Infrastructure.DateTimeProvider;
using Infrastructure.EventSourcing;
using Infrastructure.EventStorage;
using Infrastructure.Logging;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.ReadModel
{
    public class BatchEfReadModelProjector<T> : EfReadModelProjector<T>, IDisposable where T : ReadModelDbContext
    {
        private ILogLite log;
        private readonly BatchDbContextProvider<T> dbContextProvider;
        private readonly TimeSpan writeDelay;
        private readonly bool enablePermanentBatchWrites;
        private readonly TimeSpan minimunDelay;
        private BatchWriterState state = BatchWriterState.BatchWriteStopped;
        private Task? bulkWork;
        private Task? onLiveProcessingJob;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private Exception exceptionOnSave = new InvalidOperationException("Exception on save was not provided");
        private bool isLiveProcessing = false;
        private bool noEventsHasBeenReceivedYet = true;
        private Action<Checkpoint> onBatchCommited = _ => { };

        private int bulkEventsProcessedCount = 0;

        public BatchEfReadModelProjector(Func<T> contextFactory, IEventStore eventStore, IDateTimeProvider dateTime, TimeSpan writeDelay, TimeSpan minimunWriteDelay, bool enablePermanentBatchWrites = true)
           : base(contextFactory, eventStore, dateTime)
        {
            this.dbContextProvider = new BatchDbContextProvider<T>(contextFactory);
            this.log = LogManager.GetLoggerFor($"BatchEfReadModelProjector-{this.ReadModelName}");

            Ensure.Positive(writeDelay.TotalMilliseconds, "Write delay shlould be positive");
            this.writeDelay = writeDelay;
            Ensure.Positive(minimunWriteDelay.TotalMilliseconds, "Minimun write delay shlould be positive");
            this.minimunDelay = minimunWriteDelay;
            this.enablePermanentBatchWrites = enablePermanentBatchWrites;
        }

        public override async Task Project(IEvent e, Func<T, Task> projection)
        {
            if (this.noEventsHasBeenReceivedYet)
            {
                this.noEventsHasBeenReceivedYet = false;
                if (this.isLiveProcessing && !this.enablePermanentBatchWrites)
                    this.state = BatchWriterState.DirectWriteIsEnabled;
            }

            if (this.state == BatchWriterState.DirectWriteIsEnabled)
            {
                await base.Project(e, projection);
                return;
            }

            await this.semaphore.WaitAsync();
            try
            {
                switch (this.state)
                {
                    case BatchWriterState.BatchWriteIsCanceled:
                        this.state = BatchWriterState.DirectWriteIsEnabled;
                        await base.Project(e, projection);
                        break;

                    case BatchWriterState.LocalDbContextChangesHasFaulted:
                        this.state = BatchWriterState.BatchWriteIsRunning;
                        goto case BatchWriterState.BatchWriteIsRunning;

                    case BatchWriterState.BatchWriteIsRunning:
                    case BatchWriterState.BatchWriteStopped:
                        await this.QueueForBulkWrite(e, projection);
                        break;

                    case BatchWriterState.BatchDbContextSaveChangesHasFaulted:
                        this.state = BatchWriterState.BatchWriteStopped;
                        throw this.exceptionOnSave;

                    case BatchWriterState.DirectWriteIsEnabled:
                    default:
                        throw new InvalidOperationException("Could not project in this state");
                }
            }
            finally
            { this.semaphore.Release(); }
        }

        public void Dispose()
        {
            using (this.dbContextProvider)
            using (this.bulkWork)
            using (this.onLiveProcessingJob)
            {
                // dispose task
            }
        }

        public override bool BatchWritesAreEnabled =>
            !this.noEventsHasBeenReceivedYet
            && this.state != BatchWriterState.DirectWriteIsEnabled
            && this.state != BatchWriterState.BatchWriteIsCanceled;

        public override bool IsBatchWritingNow => this.state == BatchWriterState.BatchWriteIsRunning;

        public override void OnBatchCommited(Action<Checkpoint> doCheckpoint) => this.onBatchCommited = doCheckpoint;

        public override void LiveProcessingStarted()
        {
            if (this.isLiveProcessing) return;

            this.isLiveProcessing = true;
            if (this.enablePermanentBatchWrites)
            {
                this.log.Success("Fast batch writes enabled successfully");
                return;
            }

            this.onLiveProcessingJob = Task.Factory.StartNew(async () =>
            {
                this.log.Info("Waiting for batch writes before switching to direct write...");

                var delay = TimeSpan.FromSeconds(1);
                while (this.BatchWritesAreEnabled)
                {
                    this.StartBulkWriterIfNecessary(this.minimunDelay);

                    await Task.Delay(delay);
                }

                this.log.Success("Direct write enabled successfully");
            });
        }

        private async Task QueueForBulkWrite(IEvent e, Func<T, Task> projection)
        {
            try
            {
                var checkpointFromEvent = e.GetEventMetadata().GetCheckpoint();

                var cacheNeedsToBeSet = false;
                if (this.LastEventNumber != EventStream.NoEventsNumber)
                {
                    if (this.LastEventNumber >= checkpointFromEvent.EventNumber)
                    {
                        // In batch we need to checkpoint this, even if we ignore, especially in EventLog to be fully consistent at checkpoint level
                        this.onBatchCommited(checkpointFromEvent);
                        return; // fast idempotence, not even a connection was opened :D
                    }
                }
                else
                    cacheNeedsToBeSet = true;

                var context = dbContextProvider.ResolveDbContext();
                var checkpointEntity = await context.Checkpoints.FirstOrDefaultFromLocalAsync(x => x.Id == ReadModelCheckpointEntity.IDEMPOTENCY_CHK);

                if (checkpointEntity != null)
                {
                    var checkpointFromDb = new Checkpoint(new EventPosition(checkpointEntity.CommitPosition, checkpointEntity.PreparePosition), checkpointEntity.EventNumber);
                    if (cacheNeedsToBeSet)
                        this.PrepareCheckpoint(checkpointFromDb);

                    if (checkpointFromDb.EventNumber >= checkpointFromEvent.EventNumber)
                    {
                        // In batch we need to checkpoint this, even if we ignore, especially in EventLog to be fully consistent at checkpoint level
                        this.onBatchCommited(checkpointFromEvent);
                        return; // idempotence with possible cache update
                    }

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

                this.PrepareCheckpoint(checkpointFromEvent);
                this.bulkEventsProcessedCount += 1;
                this.StartBulkWriterIfNecessary(this.isLiveProcessing ? this.minimunDelay : this.writeDelay);
            }
            catch (Exception ex)
            {
                this.log.Error(ex, "An error ocurred in QueueForBulkWrite call.");
                this.state = BatchWriterState.LocalDbContextChangesHasFaulted;
                this.OnDbContextFault();
                throw;
            }
        }

        private void StartBulkWriterIfNecessary(TimeSpan delay)
        {
            if (this.state != BatchWriterState.BatchWriteStopped)
                return;

            this.state = BatchWriterState.BatchWriteIsRunning;
            this.bulkWork = Task.Factory.StartNew(async () =>
            {
                await Task.Delay(delay);

                await this.semaphore.WaitAsync();
                try
                {
                    if (this.state == BatchWriterState.LocalDbContextChangesHasFaulted)
                    {
                        this.state = BatchWriterState.BatchWriteStopped;
                        // No need to discard dbContext. See line 150;
                        //this.dbContextProvider.DiscardDbContext();
                        return;
                    }

                    this.log.Verbose($"Saving changes for {this.bulkEventsProcessedCount} processed events. Bulk events count: {this.bulkEventsProcessedCount}.");
                    var changesCount = await this.dbContextProvider.SafeSaveChangesAsync();
                    if (!isLiveProcessing)
                        this.log.Info($"Saved changes for {this.bulkEventsProcessedCount} processed events successfully. Updates made on db: {changesCount}.");

                    this.bulkEventsProcessedCount = 0;

                    if (this.isLiveProcessing && !this.enablePermanentBatchWrites)
                    {
                        this.state = BatchWriterState.BatchWriteIsCanceled;
                    }
                    else
                        this.state = BatchWriterState.BatchWriteStopped;
                    this.SetCheckpointForExtraction();
                    if (this.TryExtractCheckpoint(out var checkpoint))
                    {
                        this.onBatchCommited(checkpoint);
                    }
                    else
                        throw new InvalidOperationException("Cannot extract checkpoint. This should never happen. But it did.");
                }
                catch (Exception e)
                {
                    this.log.Error(e, "An error ocurred on bulk write commit attempt.");
                    this.exceptionOnSave = e;
                    this.state = BatchWriterState.BatchDbContextSaveChangesHasFaulted;
                    this.OnDbContextFault();
                }
                finally
                { this.semaphore.Release(); }

            });
        }

        private void OnDbContextFault()
        {
            this.DiscardCheckpoint();
            this.dbContextProvider.DiscardDbContext();
            this.bulkEventsProcessedCount = 0;
        }
    }
}
