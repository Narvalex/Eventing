using Infrastructure.EventSourcing;
using Infrastructure.Logging;
using Infrastructure.Processing;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    /// <summary>
    /// Provides basic common processing code for components that handles
    /// incoming events from a subscription.
    /// </summary>
    public sealed class EventProcessor : IDisposable
    {
        private readonly IEventDispatcher dispatcher;
        private readonly TimeSpan retryInterval;

        private IEventSubscription subscription;
        private readonly IEventSubscriptionFactory subscriptionFactory;

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ICheckpointStore checkpointStore;
        private CancellationTokenSource cancellationSource = null;
        private CancellationToken externalCancellationToken; // This is only used to ReStart recursively.
        private IDynamicThrottling dynamicThrottling;
        private readonly ILogLite logger;

        // Read modeling stuff
        private readonly bool isReadModelGenerator = false;
        private readonly IReadModelProjectionCheckpointProvider readModelCheckpointProvider;
        private readonly List<IReadModelSnapshotsSchemaInitializer> readModelSnapshotsSchemaInitializers;

        // Batch checkpointing
        private Checkpoint? lastIgnoredCheckpoint = null;
        private object batchCheckpointingSync = new object();

        private EventProcessor(ICheckpointStore checkpointStore, IEventSubscriptionFactory subscriptionFactory, IEventDispatcher dispatcher, IEventHandler eventHandler, TimeSpan retryInterval)
        {
            this.checkpointStore = Ensured.NotNull(checkpointStore, nameof(checkpointStore));
            this.subscriptionFactory = Ensured.NotNull(subscriptionFactory, nameof(subscriptionFactory));
            this.dispatcher = Ensured.NotNull(dispatcher, nameof(dispatcher));
            Ensure.NotNull(eventHandler, nameof(eventHandler));
            this.dispatcher.Register(eventHandler);
            this.retryInterval = retryInterval;
        }

        // TODO: Can implement something like an EventHandlerHub or similar: A collection of related event handlers that registers other event handler, similar to 
        // Read model projection, and allows to have a unique name for the subscription, but diferent event handler classes. 
        // This will be usefull for those Aggregates as ReadModels, where a subscription hydrates SQL Tables
        public EventProcessor(IEventHandler eventHandler, ICheckpointStore checkpointStore, IEventSubscriptionFactory subscriptionFactory, IEventDispatcher dispatcher, TimeSpan retryInterval)
            : this(checkpointStore, subscriptionFactory, dispatcher, eventHandler, retryInterval)
        {
            if (eventHandler is IReadModelProjection)
                throw new InvalidOperationException("This constructor is only for event handlers that are not ReadModelGenerators");

            this.Id = new EventProcessorId(eventHandler);
            this.logger = LogManager.GetLoggerFor(this.Id.SubscriptionName);
        }

        public EventProcessor(ICheckpointStore checkpointStore, IEventSubscriptionFactory subscriptionFactory, IEventDispatcher dispatcher, TimeSpan retryInterval, params IReadModelProjection[] readModelGenerators)
            : this(checkpointStore, subscriptionFactory, dispatcher, readModelGenerators.FirstOrDefault(), retryInterval)
        {
            if (!readModelGenerators.Any())
                throw new InvalidOperationException("Can not create event projection without at least one handler");

            readModelGenerators.Skip(1).ForEach(r => this.dispatcher.Register(r));

            this.Id = new EventProcessorId(readModelGenerators);
            this.logger = LogManager.GetLoggerFor(this.Id.SubscriptionName);

            // Readmodel checkpoint stuff
            this.isReadModelGenerator = true;
            this.readModelCheckpointProvider = readModelGenerators.First();
            this.readModelCheckpointProvider.OnBatchCommited(this.OnBatchReadModelCommited);
            this.readModelSnapshotsSchemaInitializers = readModelGenerators
                .Where(x => x is IReadModelSnapshotsSchemaInitializer)
                .Select(x => (IReadModelSnapshotsSchemaInitializer)x)
                .ToList();
        }

        public EventProcessorId Id { get; }


        /// <summary>
        /// Starts processing events
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken, IDynamicThrottling throttling)
        {
            if (this.cancellationSource != null || !cancellationToken.CanBeCanceled) return;

            this.externalCancellationToken = cancellationToken;

            var lastCheckpoint = this.checkpointStore.GetCheckpoint(this.Id);

            await this.semaphoreSlim.WaitAsync();
            try
            {
                if (this.cancellationSource == null)
                {
                    this.dynamicThrottling = Ensured.NotNull(throttling, nameof(throttling));

                    // This makes the stop effective
                    this.subscription = await this.subscriptionFactory.Create();

                    this.LiveEventProcessingStarted += this.OnLiveEventProcessingStarted;
                    this.SubscriptionDropped += this.ResilientOnSubscriptionDropped;

                    // ReadModel Snapshots Schema Initializing
                    if (this.isReadModelGenerator)
                    {
                        foreach (var init in this.readModelSnapshotsSchemaInitializers)
                            await init.InitializeReadModelSnapshotSchema();
                        this.readModelSnapshotsSchemaInitializers.Clear();
                    }

                    await this.subscription.StartAsync(
                        this.Id.SubscriptionName,
                        lastCheckpoint,
                        this.isReadModelGenerator ? (Func<IEvent, Task>)this.OnEventApearedForReadModelProjector : this.OnEventApeared,
                        this.OnEventIgnored,
                        () => this.LiveEventProcessingStarted?.Invoke(this, new LiveEventProcessingStartedEventArgs()),
                        (ex, connLost) => this.SubscriptionDropped?.Invoke(this, new SubscriptionDroppedEventArgs(ex, connLost)),
                        this.dispatcher.RegisteredEventTypes.ToArray()
                    );

                    cancellationToken.Register(async () => await this.StopAsync());
                    this.cancellationSource = new CancellationTokenSource();
                }
            }
            catch { throw; }
            finally { this.semaphoreSlim.Release(); }
        }

        public event System.EventHandler<LiveEventProcessingStartedEventArgs> LiveEventProcessingStarted;

        public event System.EventHandler<SubscriptionDroppedEventArgs> SubscriptionDropped;

        public bool IsRunning => this.cancellationSource != null;

        private async Task OnEventApeared(IEvent @event)
        {
            await this.dynamicThrottling.WaitUntilAllowedParallelism(this.cancellationSource.Token);
            this.dynamicThrottling.NotifyWorkStarted();

            var errorFlag = false;

            // Start handling with infinite retries, until cancelled or succeeded. 
            // This will make the whole system resilient and not crashing because of a process got stuck.
            await TaskRetryFactory.StartNew(
                 async () =>
                 {
                     await this.dispatcher.Dispatch(@event);
                 },
                ex =>
                {
                    this.dynamicThrottling.Penalize();

                    var metadata = @event.GetEventMetadata();

                    // Here we could mark this event processor as "failing".
                    this.logger.Error(@event, ex,
                        $"An error ocurred while processing event id {metadata.EventId} of type {@event.GetType().Name} at position {metadata.EventNumber}. Retrying in a moment...");

                    if (!errorFlag)
                        errorFlag = true;
                },
                this.retryInterval,
                cancellationSource.Token);

            this.UpdateCheckpoint(@event.GetEventMetadata().GetCheckpoint());

            if (errorFlag)
                this.dynamicThrottling.NotifyWorkCompletedWithError();
            else
                this.dynamicThrottling.NotifyWorkCompleted();
        }

        private async Task OnEventApearedForReadModelProjector(IEvent @event)
        {
            await this.dynamicThrottling.WaitUntilAllowedParallelism(this.cancellationSource.Token);
            this.dynamicThrottling.NotifyWorkStarted();

            try
            {
                await this.dispatcher.Dispatch(@event);

                if (!this.readModelCheckpointProvider.BatchWritesAreEnabled)
                    this.UpdateCheckpoint(@event.GetEventMetadata().GetCheckpoint());
                else
                    this.lastIgnoredCheckpoint = null;

                this.dynamicThrottling.NotifyWorkCompleted();
            }
            catch (Exception ex)
            {
                this.dynamicThrottling.Penalize();

                var metadata = @event.GetEventMetadata();

                // Here we could mark this event processor as "failing".
                this.logger.Error(@event, ex,
                    $"An error ocurred while processing event id {metadata.EventId} of type {@event.GetType().Name} at position {metadata.EventNumber}. Retrying in a moment...");

                this.dynamicThrottling.NotifyWorkCompletedWithError();
                // Droppping the subscription

                throw;
            }
        }

        private void OnEventIgnored(string eventType, Checkpoint checkpoint)
        {
            // We need to be efficient
            //this.logger.Verbose($"Ignored {eventType}. Position: {checkpoint.EventPosition.CommitPosition}. Event number: {checkpoint.EventNumber}");
            if (!this.isReadModelGenerator)
                this.UpdateCheckpoint(checkpoint);
            else if (this.isReadModelGenerator && !this.readModelCheckpointProvider.BatchWritesAreEnabled)
                this.UpdateCheckpoint(checkpoint);
            else if (this.isReadModelGenerator && this.readModelCheckpointProvider.BatchWritesAreEnabled && !this.readModelCheckpointProvider.IsNowBatchWriting)
                this.UpdateCheckpoint(checkpoint);
            else
            {
                lock (this.batchCheckpointingSync)
                {
                    if (this.isReadModelGenerator && this.readModelCheckpointProvider.BatchWritesAreEnabled && !this.readModelCheckpointProvider.IsNowBatchWriting)
                        this.UpdateCheckpoint(checkpoint);
                    else
                        this.lastIgnoredCheckpoint = checkpoint;
                }
            }
        }

        private void OnLiveEventProcessingStarted(object sender, LiveEventProcessingStartedEventArgs args)
        {
            var checkpoint = this.checkpointStore.GetCheckpoint(this.Id);
            this.logger.Verbose($"The event processing has caught-up on" + (checkpoint.EventNumber != EventStream.NoEventsNumber ? $" checkpoint {checkpoint.EventNumber}!" : " the very beginning!"));

            this.dispatcher.NotifyLiveProcessingStarted();
        }

        private void ResilientOnSubscriptionDropped(object sender, SubscriptionDroppedEventArgs args)
        {
            var cancellationSource = this.cancellationSource;
            if (cancellationSource == null || this.cancellationSource.IsCancellationRequested)
                throw new OperationCanceledException(this.cancellationSource.Token);

            this.dynamicThrottling.Penalize();

            this.StopAsync().Wait();

            if (args.LostConnectionReason)
            {
                this.logger.Warning($"Connection lost. Restarting now...");
            }
            else
            {
                this.logger.Error(args.Exception, $"The subscription has been dropped. Restarting subscription in a moment...");
                Task.Delay(this.retryInterval, this.externalCancellationToken).Wait();
            }

            this.StartAsync(this.externalCancellationToken, this.dynamicThrottling).Wait();
            this.logger.Success($"Event processor restarted successfully");
        }

        private void UpdateCheckpoint(Checkpoint checkpoint)
        {
            // here we could mark this event processor as "with no issues"
            // that is the reason why this method is extracted here
            this.checkpointStore.CreateOrUpdate(this.Id, checkpoint);
        }

        private void OnBatchReadModelCommited(Checkpoint checkpoint)
        {
            lock (this.batchCheckpointingSync)
            {
                if (this.lastIgnoredCheckpoint is null)
                {
                    this.UpdateCheckpoint(checkpoint);
                    return;
                }
                else if (this.lastIgnoredCheckpoint.Value.EventPosition.CommitPosition > checkpoint.EventPosition.CommitPosition)
                    this.UpdateCheckpoint(this.lastIgnoredCheckpoint.Value);
                else
                    this.UpdateCheckpoint(checkpoint);
                this.lastIgnoredCheckpoint = null;
            }
        }

        public void Dispose()
        {
            this.StopAsync().Wait();

            using (this.semaphoreSlim)
            using (this.subscription as IDisposable)
            using (this.checkpointStore as IDisposable)
            using (this.dispatcher)
            {
                // Dispose subscription if it's disposable.
            }
        }

        public async Task StopAsync()
        {
            await this.semaphoreSlim.WaitAsync();
            try
            {
                using (this.cancellationSource)
                {
                    if (this.cancellationSource != null)
                    {
                        this.cancellationSource.Cancel();

                        this.LiveEventProcessingStarted -= this.OnLiveEventProcessingStarted;
                        this.SubscriptionDropped -= this.ResilientOnSubscriptionDropped;

                        await this.subscription.Stop();
                        this.cancellationSource = null;
                    }
                }
            }
            catch { throw; }
            finally { this.semaphoreSlim.Release(); }
        }
    }
}
