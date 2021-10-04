using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing.Transactions
{
    public class TransactionCrashRecoveryEvHandler :
        IEventHandler<NewTransactionPrepareStarted>,
        IEventHandler<LockAcquired>,
        IEventHandler<RecoveredTransactionRollbackStarted>,
        IEventHandler<LockReleaseScheduled>,
        IEventHandler<EventPrepared>
    {
        private readonly IEventSourcedRepository repository;
        private readonly IEventDeserializationAndVersionManager versionManager;

        public TransactionCrashRecoveryEvHandler(IEventSourcedRepository repository, IEventDeserializationAndVersionManager versionManager)
        {
            this.repository = repository.EnsuredNotNull(nameof(repository));
            this.versionManager = versionManager.EnsuredNotNull(nameof(versionManager));
        }

        public async Task Handle(NewTransactionPrepareStarted e)
        {
            await this.repository.AwaitUntilTransactionGoesOffline(e.TransactionId);

            var txRecord = await this.repository.GetByIdAsync<TransactionRecord>(e.TransactionId);

            switch (txRecord.Status)
            {
                case TransactionStatus.Closed:
                    return; // Happy path

                case TransactionStatus.PrepareStarted:
                    txRecord.Update(e, new RecoveredTransactionRollbackStarted(e.TransactionId));
                    break;

                case TransactionStatus.CommitStarted:
                    txRecord.Update(e, new TransactionRollforwardStarted(e.TransactionId));
                    break;

                case TransactionStatus.RollbackStarted:
                    if (txRecord.RunMode == TransactionRunMode.Online)
                        txRecord.Update(e, new RecoveredTransactionRollbackStarted(e.TransactionId));
                    else return; // idepm.
                    break;

                case TransactionStatus.NotStarted:
                    throw new InvalidOperationException("The transaction status is not valid.");
            }

            await this.repository.CommitAsync(txRecord);
        }

        public async Task Handle(LockAcquired e)
        {
            var txRecord = await this.repository.GetByIdAsync<TransactionRecord>(e.LockOwnerId);
            var entityStreamName = EventStream.GetStreamName(e);
            EntityTransactionPreparation? preparation;

            switch (txRecord.Status)
            {
                case TransactionStatus.RollbackStarted:
                    preparation = await this.repository.GetByIdAsync<EntityTransactionPreparation>(entityStreamName);
                    if (preparation.IsAttachedToTransaction && preparation.TransactionId == e.LockOwnerId)
                    {
                        do
                        {
                            var batch = preparation.PreparedEventBatches.FirstOrDefault();
                            if (batch is null)
                                break;

                            preparation.Update(e, new PreparedEventsBatchCleared(entityStreamName, e.LockOwnerId, batch.Id));

                        } while (true);

                        await this.repository.CommitAsync(preparation);
                    }

                    var entity = await this.repository.TryGetByStreamNameEvenIfDoesNotExistsAsync(preparation.EntityType!.ToClrType(), entityStreamName);
                    if (entity!.Metadata.IsLocked && entity.Metadata.LockOwnerId == e.LockOwnerId)
                    {
                        entity.Update(e, new LockReleased(e.StreamId, e.LockOwnerId));
                        await this.repository.CommitAsync(entity);
                    }
                    break;

                case TransactionStatus.CommitStarted:
                    preparation = await this.repository.GetByIdAsync<EntityTransactionPreparation>(entityStreamName);
                    if (!preparation.PreparedEventBatches.Any())
                    {
                        // Entity was just locked. No prepared events found
                        preparation.Update(e, new LockReleaseScheduled(entityStreamName, e.LockOwnerId));
                        await this.repository.CommitAsync(preparation);
                        // we trust the idemp. capability of EventSourced class
                    }
                    break;

                case TransactionStatus.Closed: // Idemp
                    return;

                case TransactionStatus.NotStarted:
                case TransactionStatus.PrepareStarted:
                    throw new InvalidOperationException("The transaction status is not valid.");
            }
        }

        public async Task Handle(RecoveredTransactionRollbackStarted e)
        {
            var txRecord = await this.repository.GetByIdAsync<TransactionRecord>(e.TransactionId);
            if (txRecord.Status == TransactionStatus.RollbackStarted)
            {
                txRecord.Update(e, new RecoveredTransactionRollbackCompleted(e.TransactionId));
                await this.repository.CommitAsync(txRecord);
            }
        }

        public async Task Handle(EventPrepared e)
        {
            var preparation = await this.repository.GetByIdAsync<EntityTransactionPreparation>(e.EntityStreamName);
            if ((!preparation.IsAttachedToTransaction) || (preparation.TransactionId != e.TransactionId))
                return; // Transaction already processed

            var batch = preparation.PreparedEventBatches.FirstOrDefault(e.BatchSequenceNumber);
            if (batch is null)
                return; // The batch was already processed

            if (e.ExpectedVersion != batch.ExpectedVersion)
                return; // We only try to process events with first event in batch

            var record = await this.repository.GetByIdAsync<TransactionRecord>(e.TransactionId);
            if (record.Status != TransactionStatus.CommitStarted)
                return; // Transaction is not in commit state

            // The entity might exists in a non created state at least
            var entity = (await this.repository.TryGetByStreamNameEvenIfDoesNotExistsAsync(preparation.EntityType!.ToClrType(), e.EntityStreamName))!;
            if (!entity!.Metadata.IsLocked)
                return; // Entity already commited events
            
            // for idempotency. 
            if (entity.Metadata.Version == e.ExpectedVersion)
            {
                entity.Update(e,
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
                              e.GetEventMetadata(),
                              entity.GetEntityType()))
                      .SetTransactionId(record.Id)));

                await this.repository.CommitAsync(entity);
            }

            preparation.Update(e, new PreparedEventsBatchCleared(e.EntityStreamName, record.Id, batch.Id));
            if (!preparation.PreparedEventBatches.Any())
                preparation.Update(e, new LockReleaseScheduled(e.EntityStreamName, record.Id));

            await this.repository.CommitAsync(preparation);
        }

        public async Task Handle(LockReleaseScheduled e)
        {
            var record = await this.repository.GetByIdAsync<TransactionRecord>(e.TransactionId);
            if (record.Status == TransactionStatus.Closed)
                return;

            var preparation = await this.repository.GetByIdAsync<EntityTransactionPreparation>(e.EntityStreamName);
            // in rollforward only the locks are schedule to release
            var entity = (await this.repository.GetByStreamNameAsync(preparation.EntityType!.ToClrType(), e.EntityStreamName))!; 
            if (entity.Metadata.IsLocked && entity.Metadata.LockOwnerId == e.TransactionId)
            {
                entity.Update(e, new LockReleased(entity.Id, e.TransactionId));
                await this.repository.CommitAsync(entity);
            }

            if (record.IsLastInteractedEntity(entity))
            {
                record.Update(e, new TransactionRollforwardCompleted(e.TransactionId));
                await this.repository.CommitAsync(record);
            }
        }
    }
}
