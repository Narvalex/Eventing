using Infrastructure.Utils;

namespace Infrastructure.EventSourcing.Transactions
{

    public class TransactionRecord : EventSourced
    {
        public TransactionRecord(EventSourcedMetadata metadata, TransactionRunMode? runMode, TransactionStatus? status, TransactionOutcome? outcome, TypeObject? lastInteractedEntityType, string? lastInteractedEntityId)
            : base(metadata)
        {
            this.RunMode = runMode ?? TransactionRunMode.Online;
            this.Status = status ?? TransactionStatus.NotStarted;
            this.Outcome = outcome ?? TransactionOutcome.NotStarted;
            this.LastInteractedEntityType = lastInteractedEntityType;
            this.LastInteractedEntityId = lastInteractedEntityId;
        }

        public TransactionRunMode RunMode { get; private set; }
        public TransactionStatus Status { get; private set; }
        public TransactionOutcome Outcome { get; private set; }
        public TypeObject? LastInteractedEntityType { get; private set; }
        public string? LastInteractedEntityId { get; private set; }

        public bool IsLastInteractedEntity(IEventSourced entity) =>
            (entity.GetType().ToTypeObject() == this.LastInteractedEntityType!) && (entity.Id == this.LastInteractedEntityId);

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry
                .On<NewTransactionPrepareStarted>(_ =>
                {
                    this.Status = TransactionStatus.PrepareStarted;
                    this.Outcome = TransactionOutcome.InProgress;
                })
                .On<TransactionCommitStarted>(e =>
                {
                    this.Status = TransactionStatus.CommitStarted;
                    this.LastInteractedEntityType = e.LastInteractedEntityType;
                    this.LastInteractedEntityId = e.LastInteractedEntityId;
                })
                .On<TransactionCommitted>(_ =>
                {
                    this.Status = TransactionStatus.Closed;
                    this.Outcome = TransactionOutcome.Committed;
                })
                .On<OnlineTransactionRollbackCompleted>(_ =>
                {
                    this.Status = TransactionStatus.Closed;
                    this.Outcome = TransactionOutcome.Aborted;
                })
                .On<OnlineTransactionRollbackStarted>(_ => this.Status = TransactionStatus.RollbackStarted)
                .On<RecoveredTransactionRollbackCompleted>(_ =>
                {
                    this.Status = TransactionStatus.Closed;
                    this.Outcome = TransactionOutcome.Aborted;
                })
                .On<RecoveredTransactionRollbackStarted>(_ =>
                {
                    this.Status = TransactionStatus.RollbackStarted;
                    this.RunMode = TransactionRunMode.Recovery;
                })
                .On<TransactionRollforwardCompleted>(_ =>
                {
                    this.Status = TransactionStatus.Closed;
                    this.Outcome = TransactionOutcome.Committed;
                })
                .On<TransactionRollforwardStarted>(_ => this.RunMode = TransactionRunMode.Recovery)
            ;
    }
}
