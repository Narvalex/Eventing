using Infrastructure.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public class VolatileCheckpointStore : ICheckpointStore
    {
        private readonly ConcurrentDictionary<EventProcessorId, Checkpoint> checkpointsBySub = new ConcurrentDictionary<EventProcessorId, Checkpoint>();
        private readonly EventProcessorId eventLogId = new EventProcessorId("EventLog", EventProcessorConsts.ReadModelProjection);

        public Checkpoint GetCheckpoint(EventProcessorId id)
        {
            if (this.checkpointsBySub.TryGetValue(id, out var checkpoint))
                return checkpoint;
            else
                return Checkpoint.Start;
        }

        public Task RemoveStaleSubscriptionCheckpoints(IEnumerable<string> currentSubscriptions)
        {
            return Task.CompletedTask;
        }

        public void CreateOrUpdate(EventProcessorId id, Checkpoint checkpoint)
        {
            this.checkpointsBySub[id] = checkpoint;
        }

        public Task WaitForEventLogToBeConsistentToCommitPosition(long commitPosition)
        {
            return TaskRetryFactory.StartPolling(
                () => this.checkpointsBySub[this.eventLogId].EventPosition.CommitPosition,
                currentCommitPosition => currentCommitPosition >= commitPosition,
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromSeconds(120));
        }

        public Task WaitForEventLogToBeConsistentToEventNumber(long eventNumber)
        {
            return TaskRetryFactory.StartPolling(
                () => this.checkpointsBySub[this.eventLogId].EventNumber,
                currentEventNumber => currentEventNumber >= eventNumber,
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromSeconds(120));
        }
    }
}
