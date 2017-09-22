using System.Threading.Tasks;

namespace Eventing.Core.Persistence
{
    // This is just to give an idea of how to do it
    public interface ICheckpointRepository
    {
        long? GetCheckpoint(string subscriptionId);
        Task<long?> GetCheckpointAsync(string subscriptionId);
        void SaveCheckpoint(string subscriptionId, long checkpoint);
        Task SaveCheckpointAsync(string subscriptionId, long checkpoint);
    }
}
