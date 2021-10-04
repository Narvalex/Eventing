using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Processing
{
    public interface IExclusiveWriteLock : IDisposable
    {
        bool IsAcquired { get; }
        void StartLockAcquisitionProcess(CancellationToken token);
        Task WaitLockAcquisition(CancellationToken token);
        Task WaitLockAcquisition(TimeSpan timeout);
    }
}