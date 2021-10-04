using System;
using System.Threading.Tasks;

namespace Infrastructure.Snapshotting
{
    public interface IPersistentSnapshotterEngine : IDisposable
    {
        IPersistentSnapshotterEngine StartEngineIfNecessary();

        Task WaitUntilSnapshotsAreUpdated();
    }
}
