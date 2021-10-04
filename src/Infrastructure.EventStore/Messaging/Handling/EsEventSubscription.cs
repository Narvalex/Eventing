using EventStore.ClientAPI;
using Infrastructure.Logging;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.EventStore.Messaging.Handling
{
    public class EsEventSubscription : IEventSubscription
    {
        private bool running = false;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private readonly IEventStoreConnection resilientConnection;
        private readonly EventDeserializationAndVersionManager versionManager;
        private Func<IEvent, Task> onEventAppeared;
        private Action<string, Checkpoint> onEventIgnored;
        private Action<Exception, bool> onSubscriptionDropped;
        private Action onLiveProcessingStarted;
        private EventStoreAllCatchUpSubscription catchUpSub;
        private long lastEventNumber = -1;
        private bool handleAllEventTypes = true;

        public EsEventSubscription(IEventStoreConnection resilientConnection, EventDeserializationAndVersionManager serializer)
        {
            this.resilientConnection = Ensured.NotNull(resilientConnection, nameof(resilientConnection));
            this.versionManager = Ensured.NotNull(serializer, nameof(serializer));
        }

        public async Task StartAsync(
            string subscriptionName,
            Checkpoint lastCheckpoint,
            Func<IEvent, Task> onEventAppeared,
            Action<string, Checkpoint> onEventIgnored,
            Action onLiveProcessingStarted,
            Action<Exception, bool> onSubscriptionDropped,
            params string[] subscribedEventTypes)
        {
            await this.semaphore.WaitAsync();
            try
            {
                if (this.running)
                    return;

                this.running = true;

                Ensure.NotEmpty(subscriptionName, nameof(subscriptionName));

                var subscribedEventTypesHashSet = new HashSet<string>();
                if (subscribedEventTypes.Any())
                {
                    this.handleAllEventTypes = false;
                    subscribedEventTypesHashSet.AddRange(subscribedEventTypes);
                }

                this.onEventAppeared = Ensured.NotNull(onEventAppeared, nameof(onEventAppeared));
                this.onEventIgnored = Ensured.NotNull(onEventIgnored, nameof(onEventIgnored));
                this.onSubscriptionDropped = Ensured.NotNull(onSubscriptionDropped, nameof(onSubscriptionDropped));
                this.onLiveProcessingStarted = Ensured.NotNull(onLiveProcessingStarted, nameof(onLiveProcessingStarted));

                this.lastEventNumber = lastCheckpoint.EventNumber;

                var settings = new CatchUpSubscriptionSettings(
                    maxLiveQueueSize: 10_000, // Default
                    readBatchSize: 500, // Default
                    verboseLogging: LogManager.EnableVerbose,
                    resolveLinkTos: false,
                    subscriptionName: subscriptionName);


                this.catchUpSub = this.resilientConnection
                    .SubscribeToAllFrom(lastCheckpoint.ToEventStorePosition(), settings,
                    async (sub, resolvedEvent) =>
                    {
                        var eventType = resolvedEvent.Event.EventType;
                        var position = resolvedEvent.OriginalPosition.Value.ToEventPosition();

                        // Filters system events and Link Events
                        // More info: https://groups.google.com/forum/#!searchin/event-store/Result$20event|sort:date/event-store/SPRT0_X2M5U/N4WUYoBQAwAJ
                        // The last one check (OriginalStreamId) is just in case...
                        if (eventType.StartsWith("$") || eventType == "Result" || resolvedEvent.OriginalStreamId.StartsWith("$"))
                        {
                            this.onEventIgnored(eventType, new Checkpoint(position, this.lastEventNumber));
                            return;
                        }

                        // This is an app event, we update the event number
                        this.lastEventNumber += 1;

                        // Filter not interested events
                        if (!handleAllEventTypes && !subscribedEventTypesHashSet.Contains(eventType))
                        {
                            this.onEventIgnored(eventType, new Checkpoint(position, this.lastEventNumber));
                            return;
                        }

                        var e = resolvedEvent.ToEventForSubscription(this.lastEventNumber, this.versionManager);
                        var checkpoint = new Checkpoint(position, this.lastEventNumber);
                        e.GetEventMetadata().SetCheckpoint(checkpoint);
                        await this.onEventAppeared(e);
                    },
                    s => this.onLiveProcessingStarted(),
                    // This dropped is NEVER called before the OnAppearEvent finished event handling
                    (s, reason, ex) =>
                    {
                        if (!running || reason == SubscriptionDropReason.UserInitiated)
                            // if Stop() was called, then it will call the catchupsub to stop, Do not worry.
                            return;

                        this.Stop().Wait();
                        var message = $"The subscription was dropped. Reason {reason}. Current event number: {this.lastEventNumber}";

                        var toThrowEx = ex is null // this can be null
                            ? new EsSubscriptionDroppedException(message, reason)
                            : new EsSubscriptionDroppedException(message, reason, ex);

                        this.onSubscriptionDropped(toThrowEx, this.EvaluateIfConnectionWasLost(reason, ex?.StackTrace));
                    });
            }
            catch { throw; }
            finally { this.semaphore.Release(); }
        }

        public async Task Stop()
        {
            await this.semaphore.WaitAsync();
            try
            {
                if (!this.running)
                    return;

                this.running = false;
                this.catchUpSub.Stop();
                this.catchUpSub = null;
            }
            catch { throw; }
            finally { this.semaphore.Release(); }
        }

        private bool EvaluateIfConnectionWasLost(SubscriptionDropReason reason, string? stackTrace)
        {
            if (reason == SubscriptionDropReason.ConnectionClosed)
                return true;

            return stackTrace?.Trim() == @"
   at EventStore.ClientAPI.ClientOperations.ReadStreamEventsForwardOperation.InspectResponse(ReadStreamEventsCompleted response)
   at EventStore.ClientAPI.ClientOperations.OperationBase`2.InspectPackage(TcpPackage package)
--- End of stack trace from previous location where exception was thrown ---
   at EventStore.ClientAPI.EventStoreStreamCatchUpSubscription.ReadEventsInternalAsync(IEventStoreConnection connection, Boolean resolveLinkTos, UserCredentials userCredentials, Nullable`1 lastEventNumber)
   at EventStore.ClientAPI.EventStoreCatchUpSubscription.LoadHistoricalEventsAsync()
".Trim();
        }
    }
}
