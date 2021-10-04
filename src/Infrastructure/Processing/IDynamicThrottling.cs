using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Processing
{
    public interface IDynamicThrottling
    {
        void Start(CancellationToken cancellationToken);
        Task WaitUntilAllowedParallelism(CancellationToken cancellationToken, string? workDescription = null);
        void NotifyWorkStarted(string? workDescription = null);
        void NotifyWorkCompleted(string? workDescription = null);
        void NotifyWorkCompletedWithError(string? workDescription = null);
        void Penalize();
    }
}