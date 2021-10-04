using Infrastructure.Messaging.Handling;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Infrastructure.BackupManagement
{
    public interface IBackupManager
    {
        Task<IResponse<string>> CreateBackup(CompressionLevel compressionLevel);
    }
}
