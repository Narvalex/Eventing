using Infrastructure.DateTimeProvider;
using Infrastructure.EventSourcing.Transactions;
using Infrastructure.EventStorage;
using Infrastructure.IdGeneration;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Snapshotting;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing
{
    public class EventSourcedRepository : IEventSourcedRepository
    {
        private readonly IEventStore eventStore;
        private readonly ISnapshotRepository snapshotRepository;
        private readonly IUniqueIdGenerator idGenerator = new KestrelUniqueIdGenerator();
        private readonly IJsonSerializer serializer;
        private readonly IOnlineTransactionPool txPool;
        private readonly IEventDeserializationAndVersionManager versionManager;

        private readonly int readPageSize;
        private readonly IDateTimeProvider timeProvider;

        public EventSourcedRepository(
            IEventStore eventStore,
            ISnapshotRepository snapshotRepository,
            IDateTimeProvider timeProvider,
            IOnlineTransactionPool transactionPool,
            IJsonSerializer serializer,
            IEventDeserializationAndVersionManager versionManager,
            int readPageSize = 500
        )
        {
            this.eventStore = Ensured.NotNull(eventStore, nameof(eventStore));
            this.snapshotRepository = Ensured.NotNull(snapshotRepository, nameof(snapshotRepository));

            this.readPageSize = Ensured.Positive(readPageSize, nameof(readPageSize));
            this.timeProvider = Ensured.NotNull(timeProvider, nameof(timeProvider));
            this.txPool = transactionPool.EnsuredNotNull(nameof(transactionPool));
            this.serializer = serializer.EnsuredNotNull(nameof(serializer));
            this.versionManager = versionManager.EnsuredNotNull(nameof(versionManager));
        }

        public Task<bool> ExistsAsync<T>(string streamName) where T : class, IEventSourced =>
            this.ExistsAsync(typeof(T), streamName);

        public async Task<bool> ExistsAsync(Type type, string streamName)
        {
            if (!this.snapshotRepository.ExistsInMemory(streamName))
            {
                if (!await this.eventStore.CheckStreamExistenceAsync(streamName))
                    return false; // Quick negative result
            }

            var entity = await this.TryGetByStreamNameEvenIfDoesNotExistsAsync(type, streamName);
            return entity!.Metadata.Exists;
        }

        public async Task<T?> GetByStreamNameAsync<T>(string streamName) where T : class, IEventSourced
        {
            var entity = await TryGetByStreamNameEvenIfDoesNotExistsAsync<T>(streamName);
            return entity is not null && entity.Metadata.Exists ? entity : null;
        }

        public async Task<IEventSourced?> GetByStreamNameAsync(Type type, string streamName)
        {
            // No snapshotting here. This is used as of now just for testing
            long sliceStart = 0;
            var eventSourced = (IEventSourced)EventSourcedCreator.New(type);
            if (await eventSourced.TryRehydrate(streamName, this.eventStore, sliceStart, this.readPageSize))
                return eventSourced.Metadata.Exists ? eventSourced : null;
            else
                return null;
        }

        public async Task<T?> GetByStreamNameAsync<T>(string streamName, long maxVersion) where T : class, IEventSourced
        {
            var eventSourced = EventSourcedCreator.New<T>();
            if (await eventSourced.TryRehydrate(streamName, maxVersion, this.eventStore, 0, this.readPageSize))
            {
                return eventSourced.Metadata.Exists ? eventSourced : null;
            }
            else
                return null;
        }

        public async Task CommitAsync(IEventSourced eventSourced)
        {
            var prepareParams = eventSourced.GetPrepareEventParams();
            var newEvents = eventSourced.ExtractPendingEvents();

            var eventsCount = newEvents.Count();
            if (eventsCount < 1)
                return;

            // FK check
            await newEvents.ForEachAsync(x => x.ValidateEvent(this));

            var expectedVersion = eventSourced.Metadata.Version - eventsCount;
            // We only check the name for a new stream
            if (expectedVersion == EventStream.NoEventsNumber && eventSourced.Metadata.StreamName.HasWhiteSpace())
                throw new InvalidStreamNameException("The stream name can not contain white spaces.");

            try
            {
                await this.eventStore.AppendToStreamAsync(eventSourced.Metadata.StreamName, expectedVersion, newEvents);

                this.snapshotRepository.Save(eventSourced);
                return;
            }
            catch (OptimisticConcurrencyException)
            {
                this.snapshotRepository.InvalidateInMemorySnapshot(eventSourced.Metadata.StreamName);
                throw;
            }
        }

        public async Task AppendAsync(Type type, IEnumerable<IEventInTransit> events, IMessageMetadata incomingMetadata, string correlationId, string causationId)
        {
            events = events.ToArray();
            var eventsCount = events.Count();
            if (eventsCount < 1)
                return;

            // FK check
            await events.ForEachAsync(x => x.ValidateEvent(this));

            var streamName = EventStream.GetStreamName(type, events.First().StreamId);
            if (streamName.HasWhiteSpace())
                throw new InvalidStreamNameException($"The stream name '{streamName}' can not contain white spaces.");

            events = SetMetadata();
            await this.eventStore.AppendToStreamAsync(streamName, events);


            IEnumerable<IEventInTransit> SetMetadata()
            {
                var commitId = Guid.NewGuid().ToString();
                var firstEventId = Guid.NewGuid();
                var esType = type;
                var category = EventStream.GetCategory(esType);
                var esTypeName = esType.FullName;
                var timestamp = this.timeProvider.Now;

                var eventsWithMetadata = new List<IEvent>();

                // Since this API is only called by a command handling (not event handling because in that we need to check idempotency) we 
                // just use the default null value. No causation number.
                long? causationNumber = null;

                var firstEvent = events.First();
                firstEvent.SetEventMetadata(new EventMetadata(
                    firstEventId, correlationId, causationId, commitId, timestamp,
                    incomingMetadata.AuthorId, incomingMetadata.AuthorName, incomingMetadata.ClientIpAddress, incomingMetadata.UserAgent, causationNumber,
                    incomingMetadata.DisplayMode,
                    incomingMetadata.CommandTimestamp,
                    incomingMetadata.PositionLatitude, incomingMetadata.PositionLongitude, incomingMetadata.PositionAccuracy,
                    incomingMetadata.PositionAltitude, incomingMetadata.PositionAltitudeAccuracy, incomingMetadata.PositionHeading, incomingMetadata.PositionSpeed,
                    incomingMetadata.PositionTimestamp, incomingMetadata.PositionError), null);

                if (events.Count() > 1)
                {
                    var lastEvents = events
                        .Skip(1)
                        .Select(
                            x =>
                            {
                                x.SetEventMetadata(new EventMetadata(
                                    Guid.NewGuid(), correlationId, firstEventId.ToString(), commitId, timestamp,
                                    incomingMetadata.AuthorId, incomingMetadata.AuthorName, incomingMetadata.ClientIpAddress, incomingMetadata.UserAgent,
                                    causationNumber,
                                    incomingMetadata.DisplayMode,
                                    incomingMetadata.CommandTimestamp,
                                    incomingMetadata.PositionLatitude, incomingMetadata.PositionLongitude, incomingMetadata.PositionAccuracy,
                                    incomingMetadata.PositionAltitude, incomingMetadata.PositionAltitudeAccuracy, incomingMetadata.PositionHeading, incomingMetadata.PositionSpeed,
                                    incomingMetadata.PositionTimestamp, incomingMetadata.PositionError), null);
                                return x;
                            });

                    eventsWithMetadata.AddRange(lastEvents);
                }

                return events;
            }
        }

        public async IAsyncEnumerable<T> GetAsAsyncStream<T>() where T : class, IEventSourced
        {
            var category = EventStream.GetCategory<T>();

            long sliceStart = 0;
            CategoryStreamsSlice currentSlice;
            do
            {
                currentSlice = await this.eventStore.ReadStreamsFromCategoryAsync(category, sliceStart, this.readPageSize);

                switch (currentSlice.Status)
                {
                    case SliceFetchStatus.Success:
                        sliceStart = currentSlice.NextEventNumber;
                        foreach (var stream in currentSlice.Streams)
                        {
                            var eventSourced = await this.GetByStreamNameAsync<T>(stream.StreamName);
                            if (eventSourced is not null)
                                yield return eventSourced;
                        }
                        break;

                    case SliceFetchStatus.StreamNotFound:
                    default:
                        yield break;
                }

            } while (!currentSlice.IsEndOfStream);
        }

        public async IAsyncEnumerable<T> GetAsAsyncStream<T>(IEvent contextLimitEvent) where T : class, IEventSourced
        {
            var metadata = contextLimitEvent.GetEventMetadata();
            if (!metadata.ResolveIfEventNumberIsValid())
                throw new InvalidOperationException("The event does not have a valid EventNumber. Event numbers are valid only when comming from a subscription");

            var category = EventStream.GetCategory<T>();

            long sliceStart = 0;
            CategoryStreamsSlice currentSlice;
            do
            {
                currentSlice = await this.eventStore.ReadStreamsFromCategoryAsync(category, sliceStart, this.readPageSize, metadata.EventNumber);

                switch (currentSlice.Status)
                {
                    case SliceFetchStatus.Success:
                        sliceStart = currentSlice.NextEventNumber;
                        foreach (var stream in currentSlice.Streams)
                        {
                            var eventSourced = await this.GetByStreamNameAsync<T>(stream.StreamName, stream.Version);
                            if (eventSourced is not null)
                                yield return eventSourced!;
                        }
                        break;

                    case SliceFetchStatus.StreamNotFound:
                    default:
                        yield break;
                }

            } while (!currentSlice.IsEndOfStream);
        }

        public async Task<string> GetLastEventSourcedId<T>(int offset = 0)
        {
            var category = EventStream.GetCategory<T>();
            var streamName = await this.eventStore.ReadLastStreamFromCategory(category, offset);

            return streamName is null ? null : EventStream.GetId(streamName);
        }

        public async Task<IEventSourced?> TryGetByStreamNameEvenIfDoesNotExistsAsync(Type type, string streamName)
        {
            if (this.snapshotRepository.TryGetFromMemory(type, streamName, out var cachedSnapshot))
                return cachedSnapshot;

            long sliceStart;
            var eventSourced = await this.snapshotRepository.TryGetFromPersistentRepository(type, streamName);
            if (eventSourced != null)
                sliceStart = eventSourced.Metadata.Version + 1;
            else
            {
                sliceStart = 0;
                eventSourced = EventSourcedCreator.New(type);
            }

            if (await eventSourced.TryRehydrate(streamName, this.eventStore, sliceStart, this.readPageSize))
            {
                // we cache it, to save time next call if not exists to avoid race condition with Commit method
                this.snapshotRepository.SaveIfNotExists(eventSourced);
                return eventSourced;
            }
            else
                return null;
        }

        public async Task<T?> TryGetByStreamNameEvenIfDoesNotExistsAsync<T>(string streamName) where T : class, IEventSourced
        {
            if (this.snapshotRepository.TryGetFromMemory<T>(streamName, out var cachedSnapshot))
                return cachedSnapshot;

            long sliceStart;
            var eventSourced = await this.snapshotRepository.TryGetFromPersistentRepository<T>(streamName);
            if (eventSourced != null)
                sliceStart = eventSourced.Metadata.Version + 1;
            else
            {
                sliceStart = 0;
                eventSourced = EventSourcedCreator.New<T>();
            }

            if (await eventSourced.TryRehydrate(streamName, this.eventStore, sliceStart, this.readPageSize))
            {
                // we cache it, to save time next call if not exists to avoid race condition with Commit method
                this.snapshotRepository.SaveIfNotExists(eventSourced);
                return eventSourced;
            }
            else
                return null;
        }

        public async Task<IOnlineTransaction> NewTransaction(string correlationId, string causationId, long? causationNumber, IMessageMetadata metadata, bool isCommandMetadata)
        {
            var id = this.idGenerator.New();
            var txRecord = EventSourcedCreator.New<TransactionRecord>();
            txRecord.Update(correlationId, causationId, causationNumber, metadata, isCommandMetadata,
                new NewTransactionPrepareStarted(id));
            var prepareParams = ((IEventSourced)txRecord).GetPrepareEventParams()!;
            this.txPool.Register(id);
            await this.CommitAsync(txRecord);
            return new OnlineTransaction(this, this.txPool, this.versionManager, this.serializer, txRecord, prepareParams);
        }

        public Task AwaitUntilTransactionGoesOffline(string transactionId) => this.txPool.AwaitWhileRegistered(transactionId);
    }
}
