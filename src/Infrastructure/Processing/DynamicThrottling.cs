using Infrastructure.Logging;
using Infrastructure.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Processing
{
    /// <summary>
    /// Provides a way to throttle the work depending on the number of jobs it is able to complete and whether
    /// the job is penalized for trying to parallelize too many jobs.
    /// </summary>
    public class DynamicThrottling : IDisposable, IDynamicThrottling
    {
        private readonly ILogLite log;

        private CancellationTokenRegistration cancellationTokenRegistration;
        private bool disposeCalled = false;

        private readonly int maxDegreeOfParallelism;
        private readonly int minDegreeOfParallelism;
        private readonly int penaltyAmount;
        private readonly int workFailedPenaltyAmount;
        private readonly int workCompletedParallelismGain;
        private readonly int intervalForRestoringDegreeOfParallelism;

        private readonly AutoResetEvent waitHandle = new AutoResetEvent(true);
        private readonly Timer parallelismRestoringTimer;

        private int currentParallelJobs = 0;

        public DynamicThrottling(
            int maxDegreeOfParallelism = 230,
            int minDegreeOfParallelism = 30,
            int penaltyAmount = 3,
            int workFailedPenaltyAmount = 10,
            int workCompletedParallelismGain = 1,
            int intervalForRestoringDegreeOfParallelism = 8000
        ) : this(null, maxDegreeOfParallelism, minDegreeOfParallelism, penaltyAmount, workFailedPenaltyAmount, workCompletedParallelismGain, intervalForRestoringDegreeOfParallelism)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicThrottling"/>.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Maximum number of parallel jobs.</param>
        /// <param name="minDegreeOfParallelism">Minimum number of parallel jobs.</param>
        /// <param name="penaltyAmount">Number of degrees of parallelism to remove when penalizing slightly.</param>
        /// <param name="workFailedPenaltyAmount">Number of degrees of parallelism to remove when work fails.</param>
        /// <param name="workCompletedParallelismGain">Number of degrees of parallelism to restore on work completed.</param>
        /// <param name="intervalForRestoringDegreeOfParallelism">Interval in milliseconds to restore 1 degree of parallelism.</param>
        public DynamicThrottling(
            string? name = null,
            int maxDegreeOfParallelism = 230,
            int minDegreeOfParallelism = 30,
            int penaltyAmount = 3,
            int workFailedPenaltyAmount = 10,
            int workCompletedParallelismGain = 1,
            int intervalForRestoringDegreeOfParallelism = 8000
        )
        {
            this.maxDegreeOfParallelism = maxDegreeOfParallelism;
            this.minDegreeOfParallelism = minDegreeOfParallelism;
            this.penaltyAmount = penaltyAmount;
            this.workFailedPenaltyAmount = workFailedPenaltyAmount;
            this.workCompletedParallelismGain = workCompletedParallelismGain;
            this.intervalForRestoringDegreeOfParallelism = intervalForRestoringDegreeOfParallelism;
            this.parallelismRestoringTimer = new Timer(s => this.IncrementDegreesOfParallelism(1));

            this.AvailableDegreesOfParallelism = minDegreeOfParallelism;

            this.log = name.IsEmpty() ? LogManager.GetLoggerFor<DynamicThrottling>() : LogManager.GetLoggerFor("DynamicThrottling-" + name);
        }

        public int AvailableDegreesOfParallelism { get; private set; }

        public async Task WaitUntilAllowedParallelism(CancellationToken cancellationToken, string? description = null)
        {
            while (this.currentParallelJobs >= this.AvailableDegreesOfParallelism)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (this.log.VerboseEnabled)
                    this.log.Verbose($"{description} Waiting for available degrees of parallelism. Available: {this.AvailableDegreesOfParallelism}. In use: {this.currentParallelJobs}.");

                await this.waitHandle.WaitOneAsync();

                if (this.log.VerboseEnabled)
                    this.log.Verbose($"{description} Wait ended. Available: {this.AvailableDegreesOfParallelism}. In use: {this.currentParallelJobs}.");
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled)
                this.cancellationTokenRegistration = cancellationToken.Register(() =>
                {
                    if (!this.disposeCalled)
                        this.parallelismRestoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
                });

            this.parallelismRestoringTimer.Change(this.intervalForRestoringDegreeOfParallelism, this.intervalForRestoringDegreeOfParallelism);
            this.log.Success("Dynamic Throttling started");
        }

        public void NotifyWorkStarted(string? description = null)
        {
            if (this.disposeCalled) return;

            Interlocked.Increment(ref this.currentParallelJobs);
            if (this.log.VerboseEnabled)
                this.log.Verbose(description + " Job started. Parallel jobs are now: " + this.currentParallelJobs);
        }

        public void NotifyWorkCompleted(string? description = null)
        {
            if (this.disposeCalled) return;

            Interlocked.Decrement(ref this.currentParallelJobs);
            if (this.log.VerboseEnabled)
                this.log.Verbose(description + " Job finished. Parallel jobs are now: " + this.currentParallelJobs);
            this.IncrementDegreesOfParallelism(this.workCompletedParallelismGain);
        }

        public void Penalize()
        {
            if (this.disposeCalled) return;

            // Slightly penalize with removal of some degrees of parallelism.
            this.DecrementDegreesOfParallelism(this.penaltyAmount);
        }

        public void NotifyWorkCompletedWithError(string? description = null)
        {
            if (this.disposeCalled) return;
            // Largely penalize with removal of several degrees of parallelism.
            this.DecrementDegreesOfParallelism(this.workFailedPenaltyAmount);
            Interlocked.Decrement(ref this.currentParallelJobs);
            if (this.log.VerboseEnabled)
                this.log.Verbose(description + " Job finished with error. Parallel jobs are now: " + this.currentParallelJobs);
            this.waitHandle.Set();
        }

        public void Dispose()
        {
            this.disposeCalled = true;
            using (this.cancellationTokenRegistration) // this could be 'null'
            {
            }
            this.waitHandle.Dispose();
            this.parallelismRestoringTimer.Dispose();
        }

        private void IncrementDegreesOfParallelism(int count)
        {
            if (this.AvailableDegreesOfParallelism < this.maxDegreeOfParallelism)
            {
                this.AvailableDegreesOfParallelism += count;
                if (this.AvailableDegreesOfParallelism >= this.maxDegreeOfParallelism)
                    this.AvailableDegreesOfParallelism = this.maxDegreeOfParallelism;

                if (this.log.VerboseEnabled)
                    this.log.Verbose("Incremented available degrees of parallelism. Available: " + this.AvailableDegreesOfParallelism);
            }

            this.waitHandle.Set();
        }

        private void DecrementDegreesOfParallelism(int count)
        {
            this.AvailableDegreesOfParallelism -= count;
            if (this.AvailableDegreesOfParallelism < this.minDegreeOfParallelism)
                this.AvailableDegreesOfParallelism = minDegreeOfParallelism;

            if (this.log.VerboseEnabled)
                this.log.Verbose("Decremented available degrees of parallelism. Available: " + this.AvailableDegreesOfParallelism);
        }
    }
}
