using System.Threading.Tasks;

namespace Eventing.Core.Persistence
{
    public interface ICheckpointRepository
    {
        long? GetCheckpoint(string subscriptionId);
        Task<long?> GetCheckpointAsync(string subscriptionId);
        void SaveCheckpoint(long checkpoint);
        Task SaveCheckpointAsync(long checkpoint);
    }
}
