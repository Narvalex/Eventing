using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface IReadModelSnapshotsSchemaInitializer
    {
        Task InitializeReadModelSnapshotSchema();
    }
}
