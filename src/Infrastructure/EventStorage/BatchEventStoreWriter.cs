using Infrastructure.EventSourcing;
using Infrastructure.Logging;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.EventStorage
{
    public class BatchEventStoreWriter : IEventStore
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<BatchEventStoreWriter>();
        private readonly IEventStore eventStore;
        private readonly ConcurrentQueue<IEvent> queue = new ConcurrentQueue<IEvent>();
        private readonly int showProgressInterval;
        private int idleCounter = 0;
        private long lastEventNumber = -1;


        public BatchEventStoreWriter(IEventStore eventStore, int showProgressInterval = 500)
        {
            this.eventStore = eventStore.EnsuredNotNull(nameof(eventStore));
            this.showProgressInterval = Ensured.Positive(showProgressInterval, nameof(showProgressInterval));
        }

        public void Start(CancellationToken token)
        {
            Task.Factory.StartNew<Task>(async () =>
            {
                try
                {
                    bool showProgress = false;
                    Checkpoint? chk = null;
                    var list = new List<IEvent>();
                    while (!token.IsCancellationRequested)
                    {
                        if (this.queue.TryDequeue(out var incoming))
                        {
                            chk = incoming.GetEventMetadata().GetCheckpoint();

                            if ((chk.Value!.EventNumber % this.showProgressInterval) == 0)
                                showProgress = true;

                            // If list is empty
                            if (!list.Any())
                                list.Add(incoming);
                            // If not empty
                            else
                            {
                                var currentStreamNameInList = list.First().GetStreamName();
                                // If is the same stream name
                                if (currentStreamNameInList == incoming.GetStreamName())
                                    list.Add(incoming);
                                // If not them same stream name
                                else
                                {
                                    await this.eventStore.AppendToStreamAsync(currentStreamNameInList, list);
                                    showProgress = this.ShowProgressIfApplicable(showProgress, chk);
                                    list.Clear();
                                    list.Add(incoming);
                                }
                            }
                        }
                        // If nothing in queue but in list
                        else if (list.Any())
                        {
                            var currentStreamNameInList = list.First().GetStreamName();
                            await this.eventStore.AppendToStreamAsync(currentStreamNameInList, list);
                            showProgress = this.ShowProgressIfApplicable(showProgress, chk);
                            list.Clear();
                        }
                        // If nothing in queue nor in list
                        else
                        {
                            await this.EnterIdle(chk);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.log.Fatal(ex, $"Error on batch writing");
                    throw;
                }

            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private bool ShowProgressIfApplicable(bool showProgress, Checkpoint? chk)
        {
            if (showProgress)
            {
                this.log.Success($"Commited event number {chk?.EventNumber}");
                showProgress = false;
            }

            return showProgress;
        }

        public Task AppendToStreamAsync(string streamName, IEnumerable<IEvent> events)
        {
            if (streamName != EventStream.GetStreamName(events.First()))
                throw new InvalidOperationException($"The stream name is invalid. EventStore call stream name: {streamName}. Event metadata stream name: {EventStream.GetStreamName(events.First())}");

            foreach (var e in events)
                queue.Enqueue(e);
            return Task.CompletedTask;
        }

        public Task AppendToStreamAsync(string streamName, long expectedVersion, IEnumerable<IEvent> events)
        {
            return this.eventStore.AppendToStreamAsync(streamName, expectedVersion, events);
        }

        public Task<bool> CheckStreamExistenceAsync(string streamName)
        {
            return this.eventStore.CheckStreamExistenceAsync(streamName);
        }

        public Task<string> ReadLastStreamFromCategory(string category, int offset = 0)
        {
            return this.eventStore.ReadLastStreamFromCategory(category, offset);
        }

        public Task<EventStreamSlice> ReadStreamForwardAsync(string streamName, long from, int count)
        {
            return this.eventStore.ReadStreamForwardAsync(streamName, from, count);
        }

        public Task<CategoryStreamsSlice> ReadStreamsFromCategoryAsync(string category, long from, int count)
        {
            return this.eventStore.ReadStreamsFromCategoryAsync(category, from, count);
        }

        private async Task EnterIdle(Checkpoint? chk)
        {
            if (this.idleCounter > 60)
            {
                this.idleCounter = 0;

                if (chk.HasValue)
                {
                    if (this.lastEventNumber != chk.Value.EventNumber)
                    {
                        this.lastEventNumber = chk.Value.EventNumber;

                        // WARNING: The position is from source, not the destination, so is not usefull for checkpointing the new db subscriptions.
                        //this.log.Warning(chk, "Idling...");
                        this.log.Warning("Idling at event #" + this.lastEventNumber);
                    }
                }
            }

            // Idle
            await Task.Delay(100);
            this.idleCounter += 1;
        }

        public Task<CategoryStreamsSlice> ReadStreamsFromCategoryAsync(string category, long from, int count, long maxEventNumber)
        {
            return this.eventStore.ReadStreamsFromCategoryAsync(category, from, count, maxEventNumber);
        }
    }
}
