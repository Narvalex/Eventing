using Infrastructure.Logging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Processing
{
    public sealed class EventProcessingEngine : IDisposable
    {
        private readonly ILogLite logger = LogManager.GetLoggerFor<EventProcessingEngine>();
        private readonly IDynamicThrottling dynamicThrottling;
        private readonly IEnumerable<EventProcessor> processors;
        private readonly ICheckpointStore checkpointStore;
        private readonly IExclusiveWriteLock writeLock;
        private readonly bool enableEventProcessing;
        private readonly bool enableReadModelGeneration;
        private readonly TimeSpan retryInterval;
        private readonly Func<IEventDispatcher> dispatcherFactory = () => new PrecompiledEventDispatcher();

        public EventProcessingEngine(
            IEnumerable<IEventHandler> eventHandlers,
            IEnumerable<IEnumerable<IReadModelProjection>> readModelGenerators,
            IEventSubscriptionFactory subscriptionFactory,
            ICheckpointStore checkpointStore,
            IExclusiveWriteLock writeLock,
            TimeSpan retryInterval,
            bool enableThrottling,
            int maxParallelism,
            int minParallelism,
            bool enableEventProcessing,
            bool enableReadModelGeneration)
        {
            Ensure.Positive(retryInterval.TotalMilliseconds, nameof(retryInterval));
            this.retryInterval = retryInterval;

            this.dynamicThrottling = enableThrottling ? new DynamicThrottling(nameof(EventProcessingEngine), maxParallelism, minParallelism) as IDynamicThrottling : new NoThrottleImpl();
            this.checkpointStore = checkpointStore;
            this.writeLock = writeLock;
            this.enableEventProcessing = enableEventProcessing;
            this.enableReadModelGeneration = enableReadModelGeneration;

            // THIS INGERS SYNC DOES NOT HAVE ANY EVENT HANDLER
            //Ensure.NotEmpty(eventHandlers, nameof(eventHandlers));

            var procList = eventHandlers
                .Select(x => new EventProcessor(x,
                                this.checkpointStore,
                                subscriptionFactory,
                                this.dispatcherFactory(),
                                this.retryInterval))
                            .ToList();

            procList.AddRange(
                readModelGenerators.Select(x =>
                    new EventProcessor(
                        this.checkpointStore,
                        subscriptionFactory,
                        this.dispatcherFactory(),
                        this.retryInterval,
                        x.ToArray()))
                );


            var subNames = procList.Select(x => x.Id.SubscriptionName).ToList();
            subNames.ForEach(s =>
            {
                if (subNames.Count(x => x == s) > 1)
                    throw new InvalidOperationException($"The subscription {s} is duplicated");
            });

            this.processors = procList;
        }

        public async Task StartAsync(CancellationToken token)
        {
            if (!token.CanBeCanceled)
                return;

            if (!this.enableEventProcessing && !this.enableReadModelGeneration)
            {
                this.logger.Warning("All types of event processing are disabled. No events will be processed.");
                return;
            }

            this.logger.Info("Starting event processors...");

            if (!this.enableEventProcessing)
                this.logger.Warning("Transactional event processing is disabled");

            if (!this.enableReadModelGeneration)
                this.logger.Warning("Read model generation is disabled");

            this.dynamicThrottling.Start(token);

            var allEnabledProcessors = this.processors
                .Where(x => this.ProcessorIsEnabled(x.Id));

            var readModelProjections = allEnabledProcessors
                                        .Where(x => x.Id.Type == EventProcessorType.ReadModelProjection && x.Id.IsEventLog == false)
                                        .Select(x => x.StartAsync(token, this.dynamicThrottling));


            _ = Task.Factory.StartNew(async () =>
              {
                  this.logger.Info("Waiting write lock before starting transactional event processors...");
                  await this.writeLock.WaitLockAcquisition(token);
                  this.logger.Info("Write lock acquired. Starting transactional event processors now...");
                  var eventProcessors = allEnabledProcessors
                  .Where(x => x.Id.Type != EventProcessorType.ReadModelProjection || x.Id.IsEventLog)
                  .Select(x => x.StartAsync(token, this.dynamicThrottling));

                  await Task.WhenAll(eventProcessors);
                  this.logger.Info("Transactional event processors started successfully");

              }, token, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            await Task.WhenAll(
                readModelProjections
                .Concat(new Task[] 
                {
                    this.checkpointStore
                    .RemoveStaleSubscriptionCheckpoints(
                        this.processors
                        .Where(x => x.Id.Type != EventProcessorType.ReadModelProjection || x.Id.IsEventLog)
                        .Select(x => x.Id.SubscriptionName)) 
                })
            );

            this.logger.Info("Read model projections started successfully");
        }

        public void Dispose()
        {
            using (this.dynamicThrottling as IDisposable)
            { }

            foreach (var proc in this.processors)
            {
                using (proc)
                { }
            }
        }

        private bool ProcessorIsEnabled(EventProcessorId id)
        {
            if (id.IsEventLog && this.enableEventProcessing) return true;

            switch (id.Type)
            {
                case EventProcessorType.EventHandler:
                case EventProcessorType.EmailSender:
                case EventProcessorType.PersistentCommandHandler:
                    return this.enableEventProcessing;

                case EventProcessorType.ReadModelProjection:
                    return this.enableReadModelGeneration;

                default:
                    throw new InvalidOperationException("Not supported EventProcessorType");
            }
        }
    }
}
