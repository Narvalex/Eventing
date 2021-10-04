using Infrastructure.Messaging;
using System;

namespace Infrastructure.EventSourcing.Transactions
{
    public class EntityTransactionPreparation : EventSourced
    {
        public EntityTransactionPreparation(EventSourcedMetadata metadata, TypeObject? entityType, bool isAttachedToTransaction, string? transactionId, SubEntities<PreparedEventBatch>? preparedEventBatches)
            : base(metadata)
        {
            this.EntityType = entityType;
            this.IsAttachedToTransaction = isAttachedToTransaction;
            this.TransactionId = transactionId;
            this.PreparedEventBatches = preparedEventBatches ?? new SubEntities<PreparedEventBatch>();
        }

        public TypeObject? EntityType { get; private set; }
        public bool IsAttachedToTransaction { get; private set; }
        public string? TransactionId { get; private set; }
        public bool LockReleaseIsScheduled { get; private set; }
        public SubEntities<PreparedEventBatch> PreparedEventBatches { get; }

        protected override void OnPrepareEvent(IEvent @event)
        {
            if (@event is not EntityTransactionPreparationCreated && this.EntityType is null)
                throw new InvalidOperationException("The entity type must be registered");
        }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry
                .AddSubEntities(this.PreparedEventBatches)
                .On<EntityTransactionPreparationCreated>(e => this.EntityType = e.EntityType)
                .On<EventPrepared>(e =>
                {
                    if (!this.IsAttachedToTransaction)
                    {
                        this.IsAttachedToTransaction = true;
                        this.TransactionId = e.TransactionId;
                    }

                    if (!this.PreparedEventBatches.Any(e.BatchSequenceNumber))
                        this.PreparedEventBatches.Add(new PreparedEventBatch(e.BatchSequenceNumber, e.ExpectedVersion, new StrSubEntities2<PreparedEventDescriptor>()));

                    var descriptors = this.PreparedEventBatches[e.BatchSequenceNumber].Descriptors;
                    descriptors.Add(new PreparedEventDescriptor(e.ExpectedVersion.ToString(), e.TypeName, e.Payload));
                })
                .On<PreparedEventsBatchCleared>(e =>
                {
                    this.PreparedEventBatches.Remove(e.BatchSequenceNumber);
                    if (!this.PreparedEventBatches.Any())
                    {
                        this.IsAttachedToTransaction = false;
                        this.TransactionId = null;
                    }
                })
                .On<LockReleaseScheduled>() // We ignore this guy on purpose
            ;
    }
}
