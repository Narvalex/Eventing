using Infrastructure.EventLog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    /// <summary>
    /// Abstracts a component that gets and persists checkpoints in 
    /// a way to reduce IO roundtrips. 
    /// </summary>
    public interface ICheckpointStore : IWaitForEventLogToBeConsistent
    {
        Checkpoint GetCheckpoint(EventProcessorId id);

        // Can throw timeout exception.
        void CreateOrUpdate(EventProcessorId id, Checkpoint checkpoint);

        Task RemoveStaleSubscriptionCheckpoints(IEnumerable<string> currentSubscriptions);
    }
}
