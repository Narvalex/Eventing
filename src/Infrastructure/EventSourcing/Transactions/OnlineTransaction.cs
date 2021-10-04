using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing.Transactions
{
    public class OnlineTransaction : IOnlineTransaction
    {
        private IEventSourcedRepository repository;
        private readonly IOnlineTransactionPool pool;
        private readonly IEventDeserializationAndVersionManager versionManager;
        private readonly IJsonSerializer serializer;
        private readonly TransactionRecord txRecord;
        private int lastBatchSequenceNumber = 0;
        private UpdateEventSourcedParams prepareParams;
        private Queue<CommitBatch> commitQueue = new Queue<CommitBatch>();
        private IEventMetadata? eventMetadata;
        private Dictionary<string, Type> lockedEntitiesWithoutEvents = new Dictionary<string, Type>();
        private int maxAcquireLockRetry = 3;
        private int maxAquireLockRetryIntercalInMilliseconds = 1000;
        private (Type type, string id)? lastInteractedEntity;

        public OnlineTransaction(IEventSourcedRepository repository, IOnlineTransactionPool pool, IEventDeserializationAndVersionManager versionManager, IJsonSerializer serializer, TransactionRecord txRecord, UpdateEventSourcedParams prepareParams)
        {
            this.repository = repository;
            this.pool = pool;
            this.serializer = serializer;
            this.txRecord = txRecord;
            this.prepareParams = prepareParams;
            this.versionManager = versionManager;
        }

        public async Task<T> New<T>(string id) where T : class, IEventSourced
        {
            var streamName = EventStream.GetStreamName<T>(id);
            var entity = await this.repository.TryGetByStreamNameEvenIfDoesNotExistsAsync<T>(streamName);
            if (entity is null)
                entity = EventSourcedCreator.New<T>(); // Brand new entity
            else if (entity.Metadata.Exists)
                throw new OptimisticConcurrencyException($"The entity {streamName} already exists."); // The entity was created before

            return await this.AcquireLockAsync(entity, id); // Posible resurrection
        }

        public string TransactionId => this.txRecord.Id;

        public async Task<T> AcquireLockAsync<T>(string id) where T : class, IEventSourced
        {
            var entity = await this.repository.GetByIdAsync<T>(id);
            return await this.AcquireLockAsync(entity, id);
        }

        public async Task PrepareAsync(IEventSourced entity)
        {
            var eventSourcedType = entity.GetType();

            if (entity.GetPendingEventsCount() == 0)
                throw new InvalidOperationException("Can not prepare entity without prepared events");

            if (entity.Metadata.IsLocked && entity.Metadata.LockOwnerId != txRecord.Id)
                throw new OptimisticConcurrencyException($"The entity is participating in another transaction. Owner transaction lock id: {entity.Metadata.LockOwnerId}");

            var pendingEvents = entity.ExtractPendingEvents();
            if (this.eventMetadata is null)
                this.eventMetadata = pendingEvents.First().GetEventMetadata();

            var expectedVersion = entity.Metadata.Version - pendingEvents.Count();

            var entityInTransaction = await this.repository.TryGetByIdAsync<EntityTransactionPreparation>(entity.Metadata.StreamName);
            if (expectedVersion == EventStream.NoEventsNumber && entityInTransaction is not null)
                throw new InvalidOperationException("The expected version can not be -1 and the entity in transaction be not null");
            else if (expectedVersion == EventStream.NoEventsNumber && entityInTransaction is null)
            {
                // Brand new entity does not have yet entity transaction preparation
                entityInTransaction = EventSourcedCreator
                        .New<EntityTransactionPreparation>()
                        .Update(this.prepareParams,
                            new EntityTransactionPreparationCreated(
                                entity.Metadata.StreamName, this.txRecord.Id,
                                eventSourcedType.ToTypeObject()));

                // We need this before lock is granted;
                await this.repository.CommitAsync(entityInTransaction);
            }

            var batchSeq = this.GetNextBatchSequenceNumber();
            await pendingEvents.ForEachAsync(async e =>
            {
                await e.ValidateEvent(this.repository);

                var eventTypeName = e.GetType().Name!.WithFirstCharInLower();
                var payload = this.serializer.Serialize(e);

                entityInTransaction!.Update(this.prepareParams,
                    new EventPrepared(
                        entity.Metadata.StreamName,
                        txRecord.Id,
                        batchSeq,
                        expectedVersion,
                        eventTypeName,
                        payload)
                    );

                expectedVersion += 1;
            });

            await this.repository.CommitAsync(entityInTransaction!); // Prepared Events

            this.lockedEntitiesWithoutEvents.Remove(entity.Metadata.StreamName);
            this.commitQueue.Enqueue(new CommitBatch(batchSeq, eventSourcedType, entity.Id));
            this.lastInteractedEntity = (eventSourcedType, entity.Id);
        }

        public async Task CommitAsync()
        {
            this.txRecord.Update(this.prepareParams, new TransactionCommitStarted(this.txRecord.Id, this.lastInteractedEntity!.Value.type.ToTypeObject(), this.lastInteractedEntity!.Value.id));
            await this.repository.CommitAsync(this.txRecord);

            // Commit phase
            while (this.commitQueue.TryDequeue(out var commitBatch))
            {
                var streamName = EventStream.GetStreamName(commitBatch.EntityType, commitBatch.EntityId);

                var preparation = await this.repository.GetByIdAsync<EntityTransactionPreparation>(streamName);
                var batch = preparation.PreparedEventBatches[commitBatch.BatchSeqNumber];

                // IMPORTANT: We can not reuse an entity for multiple batches, since the expected version will conflict with not created entities
                // (see EventSourcedRepository.CommitAsync() possible exceptions
                var entity = (await this.repository.TryGetByStreamNameEvenIfDoesNotExistsAsync(commitBatch.EntityType, streamName))!;
                if (entity.Metadata.Version != batch.ExpectedVersion)
                    throw new InvalidOperationException("The expected version do not match!");
                entity.Update(this.prepareParams,
                    batch
                    .Descriptors
                    .AsEnumerable()
                    .Select(x =>
                        ((IEventInTransit)
                            this.versionManager
                            .GetLatestEventVersion(
                                x.TypeName,
                                default(long),
                                default(long),
                                x.Payload,
                                this.eventMetadata!,
                                entity.GetEntityType()))
                        .SetTransactionId(this.txRecord.Id)));

                await this.repository.CommitAsync(entity);

                preparation.Update(this.prepareParams, new PreparedEventsBatchCleared(streamName, this.txRecord.Id, commitBatch.BatchSeqNumber));
                await this.repository.CommitAsync(preparation);

                if (!this.lockedEntitiesWithoutEvents.ContainsKey(entity.Metadata.StreamName))
                    this.lockedEntitiesWithoutEvents.Add(entity.Metadata.StreamName, commitBatch.EntityType);
            }

            // Release phase
            do
            {
                var lockedEntity = this.lockedEntitiesWithoutEvents.First();

                var entity = (await this.repository.GetByStreamNameAsync(lockedEntity.Value, lockedEntity.Key))!;
                entity.Update(this.prepareParams, new LockReleased(entity.Id, this.txRecord.Id));
                await this.repository.CommitAsync(entity);

                this.lockedEntitiesWithoutEvents.Remove(lockedEntity.Key);

            } while (this.lockedEntitiesWithoutEvents.Any());

            this.txRecord.Update(this.prepareParams, new TransactionCommitted(this.txRecord.Id));
            await this.repository.CommitAsync(this.txRecord);

            this.pool.Unregister(this.txRecord.Id);
        }

        public async Task Rollback()
        {
            if (this.txRecord.Status == TransactionStatus.Closed)
                return; // Transaction already closed.

            if (this.txRecord.Status != TransactionStatus.PrepareStarted)
                throw new InvalidOperationException("Invalid transaction status");

            // Rollback status declared
            this.txRecord.Update(this.prepareParams, new OnlineTransactionRollbackStarted(this.txRecord.Id));
            await this.repository.CommitAsync(this.txRecord);

            // Clearing prepared events
            while (this.commitQueue.TryDequeue(out var commitBatch))
            {
                var streamName = EventStream.GetStreamName(commitBatch.EntityType, commitBatch.EntityId);

                var preparation = await this.repository.GetByIdAsync<EntityTransactionPreparation>(streamName);
                preparation.Update(this.prepareParams, new PreparedEventsBatchCleared(streamName, this.txRecord.Id, commitBatch.BatchSeqNumber));
                await this.repository.CommitAsync(preparation);

                if (!this.lockedEntitiesWithoutEvents.ContainsKey(streamName))
                    this.lockedEntitiesWithoutEvents.Add(streamName, commitBatch.EntityType);
            }

            // Release locks phase
            while (this.lockedEntitiesWithoutEvents.Any())
            {
                var commitedEntity = this.lockedEntitiesWithoutEvents.First();

                var entity = (await this.repository.TryGetByStreamNameEvenIfDoesNotExistsAsync(commitedEntity.Value, commitedEntity.Key))!;
                entity.Update(this.prepareParams, new LockReleased(entity.Id, this.txRecord.Id));
                await this.repository.CommitAsync(entity);

                this.lockedEntitiesWithoutEvents.Remove(commitedEntity.Key);

            }

            this.txRecord.Update(this.prepareParams, new OnlineTransactionRollbackCompleted(this.txRecord.Id));
            await this.repository.CommitAsync(this.txRecord);

            this.pool.Unregister(this.txRecord.Id);
        }

        public void Dispose()
        {
            try
            {
                this.Rollback().Wait();
            }
            finally
            {
                this.lockedEntitiesWithoutEvents.Clear();
                this.commitQueue.Clear();
            }
        }

        private int GetNextBatchSequenceNumber() => this.lastBatchSequenceNumber += 1;

        // The id parameter is required because a not created entity could be passed in here
        private async Task<T> AcquireLockAsync<T>(T entity, string id) where T : class, IEventSourced
        {
            var attempsCount = 0;
            Random? random = null;
            do
            {
                try
                {
                    attempsCount += 1;
                    if (attempsCount > 1)
                        entity = await this.repository.GetByIdAsync<T>(id);

                    if (entity.Metadata.IsLocked)
                        throw new OptimisticConcurrencyException($"The entity {entity.Metadata.StreamName} is participating in another transaction. Owner transaction lock id: {entity.Metadata.LockOwnerId}");

                    var entityType = typeof(T);
                    if (!await this.repository.ExistsAsync<EntityTransactionPreparation>(EventStream.GetStreamName<EntityTransactionPreparation>(entity.Metadata.StreamName)))
                    {
                        await this.repository.CommitAsync(
                            EventSourcedCreator
                                .New<EntityTransactionPreparation>()
                                .Update(
                                    this.prepareParams,
                                    new EntityTransactionPreparationCreated(
                                        EventStream.GetStreamName<T>(id), this.txRecord.Id, entityType.ToTypeObject()))
                        );
                    }


                    entity.Update(this.prepareParams, new LockAcquired(id, txRecord.Id));
                    await this.repository.CommitAsync(entity);
                    this.lastInteractedEntity = (entityType, id);

                    this.lockedEntitiesWithoutEvents.Add(EventStream.GetStreamName(entityType, id), entityType);
                    return entity;
                }
                catch (OptimisticConcurrencyException)
                {
                    if (attempsCount > this.maxAcquireLockRetry)
                        throw;
                    else
                    {
                        if (random is null)
                            random = new Random();
                        await Task.Delay(random.Next(0, this.maxAquireLockRetryIntercalInMilliseconds));
                    }
                }
            } while (true);
        }
    }
}
