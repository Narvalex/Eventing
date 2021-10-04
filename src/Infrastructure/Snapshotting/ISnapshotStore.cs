using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Snapshotting
{
    public interface ISnapshotStore
    {
        Task<IList<SnapshotSchema>> GetSchemas();
        Task<SnapshotData?> TryGetFirstStaleSnapshot(string type, int schemaVersion);
        Task<SnapshotData?> TryGetSnapshot(string streamName, int schemaVersion);
        Task Save(params SnapshotSchema[] schemas);
        Task Save(params SnapshotData[] snapshots);
    }
}
