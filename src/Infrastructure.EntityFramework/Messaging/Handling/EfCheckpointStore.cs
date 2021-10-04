using Infrastructure.DateTimeProvider;
using Infrastructure.EntityFramework.Messaging.Handling.Database;
using Infrastructure.EntityFramework.ReadModel;
using Infrastructure.Logging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.Messaging.Handling
{
    public class EfCheckpointStore : ICheckpointStore, IReadModelCheckpointStoreRegistry, IDisposable
    {
        private readonly ILogLite logger = LogManager.GetLoggerFor<EfCheckpointStore>();

        private readonly Func<CheckpointStoreDbContext> writeContextFactory;
        private readonly Func<CheckpointStoreDbContext> readContextFactory;
        private readonly Lazy<ConcurrentDictionary<EventProcessorId, Checkpoint>> lazyCheckpointsByProcessorId;
        private readonly EventProcessorId eventLogId = new EventProcessorId("EventLog", EventProcessorConsts.ReadModelProjection);

        private ConcurrentQueue<KeyValuePair<EventProcessorId, Checkpoint>> pendingUpdates = new ConcurrentQueue<KeyValuePair<EventProcessorId, Checkpoint>>();

        private CancellationTokenSource tokenSource;
        private readonly IDateTimeProvider dateTime;
        private readonly TimeSpan pollDelay;
        private readonly TimeSpan idleDelay;
        private readonly TimeSpan consistencyTimeout = TimeSpan.FromSeconds(120);

        // Read models
        private readonly ConcurrentDictionary<string, Func<ReadModelDbContext>> readModelFactoriesByName = new ConcurrentDictionary<string, Func<ReadModelDbContext>>();

        public EfCheckpointStore(Func<CheckpointStoreDbContext> writeContextFactory, Func<CheckpointStoreDbContext> readContextFactory, IDateTimeProvider dateTime, TimeSpan pollDelay, TimeSpan idleDelay)
        {
            this.writeContextFactory = Ensured.NotNull(writeContextFactory, nameof(writeContextFactory));
            this.readContextFactory = Ensured.NotNull(readContextFactory, nameof(readContextFactory));

            this.lazyCheckpointsByProcessorId = new Lazy<ConcurrentDictionary<EventProcessorId, Checkpoint>>(() => this.GetPersistedCheckpoints());

            Ensure.NotNegative(pollDelay.TotalMilliseconds, nameof(pollDelay));
            Ensure.NotNegative(idleDelay.TotalMilliseconds, nameof(idleDelay));
            this.pollDelay = pollDelay;
            this.idleDelay = idleDelay;
            this.dateTime = Ensured.NotNull(dateTime, nameof(dateTime));

            this.tokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(
                async () => await this.PersistCheckpoints(this.tokenSource.Token),
                this.tokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Current);
        }

        public Checkpoint GetCheckpoint(EventProcessorId id)
        {
            if (this.lazyCheckpointsByProcessorId.Value.ContainsKey(id))
                return this.lazyCheckpointsByProcessorId.Value[id];
            else
                return Checkpoint.Start;
        }

        public void CreateOrUpdate(EventProcessorId id, Checkpoint checkpoint)
        {
            // Cache the checkpoint
            this.lazyCheckpointsByProcessorId.Value[id] = checkpoint;
            this.pendingUpdates.Enqueue(new KeyValuePair<EventProcessorId, Checkpoint>(id, checkpoint));
        }

        public async Task PersistCheckpoints(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (this.pendingUpdates.IsEmpty)
                    await Task.Delay(this.idleDelay, token);
                else
                {
                    await this.DoPersistCheckpoints();
                    await Task.Delay(this.pollDelay, token);
                }
            }
        }

        public async Task RemoveStaleSubscriptionCheckpoints(IEnumerable<string> currentSubscriptions)
        {
            using (var context = this.readContextFactory())
            {
                var foundSubscriptions = await Queryable.Select(context.Checkpoints, x => x.SubscriptionId).ToListAsync();

                if (!foundSubscriptions.Any())
                    return;

                if (foundSubscriptions.All(x => currentSubscriptions.Contains(x)))
                    return;
            }

            using (var context = this.writeContextFactory())
            {
                var currentSubs = await context.Checkpoints.ToListFromStreamAsync();
                var count = 0;
                foreach (var entity in currentSubs)
                {
                    if (!currentSubscriptions.Contains(entity.SubscriptionId))
                    {
                        count += 1;
                        this.logger.Warning("Removing stale subscription " + entity.SubscriptionId);
                        context.Checkpoints.Remove(entity);
                    }
                }

                await context.SaveChangesAsync();

                this.logger.Warning($"Removed {count} stale subscription/s");
            }
        }

        public void Dispose()
        {
            using (this.tokenSource)
            {
                if (this.tokenSource != null)
                {
                    this.tokenSource.Cancel();
                    this.tokenSource.Dispose();
                    this.tokenSource = null!;
                }
            }
        }

        private async Task DoPersistCheckpoints()
        {
            var dictionary = new Dictionary<EventProcessorId, Checkpoint>();
            while (this.pendingUpdates.TryDequeue(out var current))
            {
                // If consumers are too fast, this will be an endless loop

                dictionary[current.Key] = current.Value;
            }

            try
            {
                using (var context = this.writeContextFactory())
                {
                    foreach (var item in dictionary.Where(x => x.Key.Type != EventProcessorType.ReadModelProjection || x.Key.IsEventLog))
                    {
                        var entity = await context.Checkpoints.FirstOrDefaultAsync(x => x.SubscriptionId == item.Key.SubscriptionName);
                        if (entity is null)
                        {
                            context.Checkpoints.Add(new CheckpointEntity
                            {
                                SubscriptionId = item.Key.SubscriptionName,
                                Type = this.lazyCheckpointsByProcessorId.Value.Keys.First(k => k.SubscriptionName == item.Key.SubscriptionName).TypeDescription,
                                EventNumber = item.Value.EventNumber,
                                CommitPosition = item.Value.EventPosition.CommitPosition,
                                PreparePosition = item.Value.EventPosition.PreparePosition,
                                TimeStamp = this.dateTime.Now
                            });
                        }
                        else
                        {
                            entity.EventNumber = item.Value.EventNumber;
                            entity.CommitPosition = item.Value.EventPosition.CommitPosition;
                            entity.PreparePosition = item.Value.EventPosition.PreparePosition;
                            entity.TimeStamp = this.dateTime.Now;
                        }
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // we catch all exceptions in order to be resilient
                this.logger.Error(ex, "An exception ocurred in checkpont repository with event handlers. Ignoring it...");
            }

            try
            {
                foreach (var item in dictionary.Where(x => x.Key.Type == EventProcessorType.ReadModelProjection && !x.Key.IsEventLog))
                {
                    using (var context = this.readModelFactoriesByName[item.Key.SubscriptionName].Invoke())
                    {
                        var entity = await context.Checkpoints.FirstOrDefaultAsync(x => x.Id == ReadModelCheckpointEntity.SUBSCRIPTION_CHK);
                        if (entity is null)
                        {
                            context.Checkpoints.Add(new ReadModelCheckpointEntity
                            {
                                Id = ReadModelCheckpointEntity.SUBSCRIPTION_CHK,
                                EventNumber = item.Value.EventNumber,
                                CommitPosition = item.Value.EventPosition.CommitPosition,
                                PreparePosition = item.Value.EventPosition.PreparePosition,
                                TimeStamp = this.dateTime.Now
                            });
                        }
                        else
                        {
                            entity.EventNumber = item.Value.EventNumber;
                            entity.CommitPosition = item.Value.EventPosition.CommitPosition;
                            entity.PreparePosition = item.Value.EventPosition.PreparePosition;
                            entity.TimeStamp = this.dateTime.Now;
                        }

                        await context.UnsafeSaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // we catch all exceptions in order to be resilient
                this.logger.Error(ex, "An exception ocurred in checkpont repository with read models. Ignoring it...");
            }
        }

        private ConcurrentDictionary<EventProcessorId, Checkpoint> GetPersistedCheckpoints()
        {
            Dictionary<EventProcessorId, Checkpoint> persistedCheckpoints;
            using (var context = this.readContextFactory())
            {
                persistedCheckpoints = context
                    .Checkpoints
                    .ToArray()
                    .ToDictionary(
                        x => new EventProcessorId(x.SubscriptionId, x.Type),
                        x => new Checkpoint(
                                new EventPosition(
                                    x.CommitPosition,
                                    x.PreparePosition),
                                x.EventNumber));
            }

            foreach (var item in this.readModelFactoriesByName)
            {
                using (var context = item.Value.Invoke())
                {
                    var entity = context.Checkpoints.FirstOrDefault(x => x.Id == ReadModelCheckpointEntity.SUBSCRIPTION_CHK);
                    persistedCheckpoints.Add(
                        new EventProcessorId(item.Key, EventProcessorConsts.ReadModelProjection),
                        entity is null ? Checkpoint.Start : new Checkpoint(new EventPosition(entity.CommitPosition, entity.PreparePosition), entity.EventNumber)
                    );
                }
            }

            return new ConcurrentDictionary<EventProcessorId, Checkpoint>(persistedCheckpoints);
        }

        public Task WaitForEventLogToBeConsistentToCommitPosition(long commitPosition) =>
            TaskRetryFactory.StartPolling(
                () => this.lazyCheckpointsByProcessorId.Value[this.eventLogId].EventPosition.CommitPosition,
                currentCommitPosition => currentCommitPosition >= commitPosition,
                TimeSpan.FromMilliseconds(1),
                this.consistencyTimeout);

        public Task WaitForEventLogToBeConsistentToEventNumber(long eventNumber) =>
            TaskRetryFactory.StartPolling(
                () => this.lazyCheckpointsByProcessorId.Value[this.eventLogId].EventNumber,
                currentEventNumber => currentEventNumber >= eventNumber,
                TimeSpan.FromMilliseconds(1),
                this.consistencyTimeout);

        public void Register(IEfDbInitializer initializer)
        {
            var readModelName = initializer.TryGetReadModelName();
            this.readModelFactoriesByName[readModelName] = initializer.TryGetReadModelContextFactory();
        }
    }
}
