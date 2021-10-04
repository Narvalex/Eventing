using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Processing
{
    public class NoThrottleImpl : IDynamicThrottling
    {
        public void NotifyWorkCompleted(string? description = null)
        {
        }

        public void NotifyWorkCompletedWithError(string? description = null)
        {
        }

        public void NotifyWorkStarted(string? description = null)
        {
        }

        public void Penalize()
        {
        }

        public void Start(CancellationToken cancellationToken)
        {
        }

        public Task WaitUntilAllowedParallelism(CancellationToken cancellationToken, string? description = null)
        {
            return Task.CompletedTask;
        }
    }
}