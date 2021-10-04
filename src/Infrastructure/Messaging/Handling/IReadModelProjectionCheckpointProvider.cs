using System;

namespace Infrastructure.Messaging.Handling
{
    public interface IReadModelProjectionCheckpointProvider
    {
        bool BatchWritesAreEnabled { get; }

        bool IsNowBatchWriting { get; }

        void OnBatchCommited(Action<Checkpoint> checkpoint);
    }
}
