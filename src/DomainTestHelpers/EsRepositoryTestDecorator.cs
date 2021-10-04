using Infrastructure.EventSourcing;
using Infrastructure.EventSourcing.Transactions;
using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Snapshotting;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Erp.Domain.Tests.Helpers
{
    public class EsRepositoryTestDecorator : IEventSourcedRepository
    {
        private readonly IEventSourcedRepository repository;
        private readonly IJsonSerializer serializer;
        private readonly ISnapshotRepository cache;
        private readonly ModelSerializationTestHelper serializationTest;
        private readonly IOnlineTransactionPool pool;
        private readonly IEventDeserializationAndVersionManager versionManager;

        public EsRepositoryTestDecorator(IJsonSerializer serializer, IEventSourcedRepository repository, ISnapshotRepository cache, IOnlineTransactionPool pool, IEventDeserializationAndVersionManager versionManager)
        {
            this.repository = Ensured.NotNull(repository, nameof(repository));
            this.serializer = Ensured.NotNull(serializer, nameof(serializer));
            this.cache = Ensured.NotNull(cache, nameof(cache));
            this.serializationTest = new ModelSerializationTestHelper(this.serializer);
            this.pool = pool.EnsuredNotNull(nameof(pool));
            this.versionManager = versionManager.EnsuredNotNull(nameof(versionManager));
        }

        public List<IEvent> LastEvents { get; private set; } = new List<IEvent>();

        public Task<bool> ExistsAsync<T>(string streamName) where T : class, IEventSourced
        {
            return this.repository.ExistsAsync<T>(streamName);
        }

        public Task<bool> ExistsAsync(Type type, string streamName)
        {
            return this.repository.ExistsAsync(type, streamName);
        }

        public Task<T?> GetByStreamNameAsync<T>(string streamName) where T : class, IEventSourced
        {
            return this.repository.GetByStreamNameAsync<T>(streamName);
        }

        /// <summary>
        /// This api is only for the given api in testable handlers. This uses the snapshoting capabilities of the 
        /// repository, and for that, it tests if the event sourced really can rehaydrate all its properties from 
        /// constructor, that is, from a serialized snapshot.
        /// </summary>
        public async Task<T> CommitAsyncForGiven<T>(T eventSourced) where T : class, IEventSourced
        {
            this.serializationTest.EnsureSerializationIsValid(eventSourced);
            return await this.repository.CommitAsync<T>(eventSourced);
        }

        public async Task CommitAsync(IEventSourced eventSourced)
        {
            this.serializationTest.EnsureSerializationIsValid(eventSourced);

            var events = eventSourced.ExtractPendingEvents();
            if (events.Count() < 1)
                return;

            var incomingMetadata = events.First().GetEventMetadata();

            // This call just appends stuff
            await this.PrivateAppendAsync(eventSourced.GetType(), events, incomingMetadata, incomingMetadata.CorrelationId, incomingMetadata.CausationId, eventSourced is IMutexSagaExecutionCoordinator, true, eventSourced.Metadata.Version - events.Count());
            // since the above call wont cache, we cache it now
            this.cache.Save(eventSourced);

            return;
        }

        private async Task PrivateAppendAsync(Type entityType, IEnumerable<IEventInTransit> events, IMessageMetadata incomingMetadata, string correlationId, string causationId, bool isMutex = false, bool concurrencyCheck = false, long? expectedVersion = null)
        {
            if (concurrencyCheck)
            {
                try
                {
                    var eventSourced = await this.repository.TryGetByStreamNameEvenIfDoesNotExistsAsync(entityType, EventStream.GetStreamName(entityType, events.First().StreamId));
                    var actualVersion = eventSourced == null ? -1 : eventSourced.Metadata.Version;
                    if (actualVersion != expectedVersion)
                        throw new OptimisticConcurrencyException();
                }
                catch (OptimisticConcurrencyException)
                {
                    if (events.Any(x => x is EntityEvent))
                        throw;

                    if (expectedVersion != EventStream.NoEventsNumber)
                        throw;

                    var fetchedEventSourced = await this.TryGetByStreamNameEvenIfDoesNotExistsAsync(entityType, EventStream.GetStreamName(entityType, events.First().StreamId));

                    if (fetchedEventSourced is null || fetchedEventSourced.Metadata.Exists || fetchedEventSourced.Metadata.IsLocked)
                        throw;
                }
            }

            await events.ForEachAsync(async e =>
            {
                this.serializationTest.EnsureSerializationIsValid(e);

                await this.repository.AppendAsync(entityType, new IEventInTransit[] { e }, incomingMetadata, isMutex ? e.GetCorrelationId() : correlationId, causationId);
            });

            this.LastEvents.AddRange(events);
        }


        public void ClearLastEvents() => this.LastEvents.Clear();

        public Task<string> GetLastEventSourcedId<T>(int offset = 0)
            => this.repository.GetLastEventSourcedId<T>(offset);

        IAsyncEnumerable<T> IEventSourcedReader.GetAsAsyncStream<T>()
            => this.repository.GetAsAsyncStream<T>();

        public Task<IEventSourced?> GetByStreamNameAsync(Type type, string streamName)
        {
            return this.repository.GetByStreamNameAsync(type, streamName);
        }

        IAsyncEnumerable<T> IEventSourcedReader.GetAsAsyncStream<T>(IEvent e)
        {
            return repository.GetAsAsyncStream<T>(e);
        }

        public Task<T?> GetByStreamNameAsync<T>(string streamName, long maxVersion) where T : class, IEventSourced
        {
            // This method is not used in command/event handling
            return repository.GetByStreamNameAsync<T>(streamName, maxVersion);
        }

        Task<T> IEventSourcedReader.TryGetByStreamNameEvenIfDoesNotExistsAsync<T>(string streamName)
        {
            return this.repository.TryGetByStreamNameEvenIfDoesNotExistsAsync<T>(streamName);
        }

        public Task<IEventSourced?> TryGetByStreamNameEvenIfDoesNotExistsAsync(Type type, string streamName)
        {
            return this.repository.TryGetByStreamNameEvenIfDoesNotExistsAsync(type, streamName);
        }

        public Task AppendAsync(Type type, IEnumerable<IEventInTransit> events, IMessageMetadata incomingMetadata, string correlationId, string causationId) =>
            this.PrivateAppendAsync(type, events, incomingMetadata, correlationId, causationId);

        public async Task<IOnlineTransaction> NewTransaction(string correlationId, string causationId, long? causationNumber, IMessageMetadata metadata, bool isCommandMetadata)
        {
            var id = Guid.NewGuid().ToString();
            var txRecord = EventSourcedCreator.New<TransactionRecord>();
            txRecord.Update(correlationId, causationId, causationNumber, metadata, isCommandMetadata,
                new NewTransactionPrepareStarted(id));
            var prepareParams = ((IEventSourced)txRecord).GetPrepareEventParams()!;
            await this.CommitAsync(txRecord);
            return new OnlineTransaction(this, this.pool, this.versionManager, this.serializer, txRecord, prepareParams);

        }

        public Task AwaitUntilTransactionGoesOffline(string transactionId) => this.repository.AwaitUntilTransactionGoesOffline(transactionId);
    }
}
