using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Processing.WriteLock
{
    public class NoExclusiveWriteLock : IExclusiveWriteLock
    {
        public bool IsAcquired => true;

        public void Dispose() { }

        public void StartLockAcquisitionProcess(CancellationToken token) { }

        public Task WaitLockAcquisition(CancellationToken token) => Task.CompletedTask;

        public Task WaitLockAcquisition(TimeSpan timeout) => Task.CompletedTask;
    }
}
