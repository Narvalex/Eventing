using Infrastructure.EntityFramework.EventStorage.Database;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.Messaging.Handling
{
    public class EfEventSubscription : IEventSubscription, IDisposable
    {
        private bool disposed = false;

        private readonly Func<EventStoreDbContext> readContextFactory;
        private readonly EventDeserializationAndVersionManager serializer;
        private Task receiverTask;
        private CancellationTokenSource tokenSource;
        private readonly TimeSpan pollDelay;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private long lastCheckpoint;

        private Func<IEvent, Task> onEventAppeared;
        private Action<string, Checkpoint> onEventIgnored;
        private Action<Exception, bool> onSubscriptionDropped;
        private LiveProcessingHandler liveProcessingHandler;

        private HashSet<string> subscribedEventTypes;
        private bool handleAllEventTypes = true;

        public EfEventSubscription(Func<EventStoreDbContext> readContextFactory, EventDeserializationAndVersionManager serializer, TimeSpan pollDelay)
        {
            this.readContextFactory = Ensured.NotNull(readContextFactory, nameof(readContextFactory));
            this.serializer = Ensured.NotNull(serializer, nameof(serializer));

            Ensure.NotNegative(pollDelay.TotalMilliseconds, nameof(pollDelay));
            this.pollDelay = pollDelay;
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
                if (this.tokenSource != null)
                    return;

                this.lastCheckpoint = lastCheckpoint.EventNumber;
                this.onEventAppeared = Ensured.NotNull(onEventAppeared, nameof(onEventAppeared));
                this.onEventIgnored = Ensured.NotNull(onEventIgnored, nameof(onEventIgnored));
                this.onSubscriptionDropped = Ensured.NotNull(onSubscriptionDropped, nameof(onSubscriptionDropped));
                this.liveProcessingHandler = new LiveProcessingHandler(onLiveProcessingStarted);

                Ensure.NotEmpty(subscriptionName, nameof(subscriptionName));

                if (subscribedEventTypes.Any())
                {
                    this.subscribedEventTypes = new HashSet<string>(subscribedEventTypes);
                    this.handleAllEventTypes = false;
                }

                this.tokenSource = new CancellationTokenSource();
                this.receiverTask = Task.Factory.StartNew(
                    async () => await this.ReceiveEvents(this.tokenSource.Token),
                    this.tokenSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Current);
            }
            catch { throw; }
            finally { this.semaphore.Release(); }
        }

        public async Task Stop()
        {
            await this.semaphore.WaitAsync();
            try
            {
                using (this.tokenSource)
                {
                    // one can actually dispose a null object. Wow.
                    if (this.tokenSource != null)
                    {
                        this.tokenSource.Cancel();
                        this.receiverTask = null;
                        this.tokenSource = null;
                    }
                }
            }
            catch { throw; }
            finally { this.semaphore.Release(); }
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.Stop().Wait();

            using (this.semaphore)
            {
                this.disposed = true;
            }
        }

        /// <summary>
        /// Receive events in a endless loop.
        /// </summary>
        private async Task ReceiveEvents(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (await this.ReceiveEvent())
                    continue;

                await Task.Delay(this.pollDelay, cancellationToken);
            }
        }

        private async Task<bool> ReceiveEvent()
        {
            try
            {
                using (var context = this.readContextFactory())
                {
                    var currentPosition = this.lastCheckpoint + 1;
                    var descriptor = await context.Events.FirstOrDefaultAsync(x => x.Position == currentPosition);
                    if (descriptor is null)
                    {
                        this.liveProcessingHandler.EnterLiveProcessingIfApplicable(this.lastCheckpoint);
                        return false;
                    }

                    this.lastCheckpoint = currentPosition;
                    var checkpoint = new Checkpoint(new EventPosition(this.lastCheckpoint, this.lastCheckpoint), this.lastCheckpoint);

                    if (!handleAllEventTypes && !subscribedEventTypes.Contains(descriptor.EventType))
                    {
                        // Filter not interested events
                        this.onEventIgnored(descriptor.EventType, checkpoint);
                    }
                    else
                    {
                        // Handle it
                        var e = this.serializer
                                .GetLatestEventVersion(
                                    descriptor.EventType,
                                    descriptor.Version,
                                    descriptor.Position,
                                    descriptor.Payload,
                                    descriptor.Metadata,
                                    descriptor.Category);

                        e.GetEventMetadata().SetCheckpoint(checkpoint);
                        await this.onEventAppeared(e);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                // TODO: catch connection lost exception
                await this.Stop();
                this.onSubscriptionDropped(ex, false);
                throw; 
            }
        }
    }
}
