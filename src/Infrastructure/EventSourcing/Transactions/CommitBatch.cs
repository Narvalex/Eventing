using System;

namespace Infrastructure.EventSourcing.Transactions
{
    public class CommitBatch
    {
        public CommitBatch(int batchSeqNumber, Type entityType, string entityId)
        {
            this.BatchSeqNumber = batchSeqNumber;
            this.EntityType = entityType;
            this.EntityId = entityId;
        }

        public int BatchSeqNumber { get; }
        public Type EntityType { get; }
        public string EntityId { get; }
    }
}
