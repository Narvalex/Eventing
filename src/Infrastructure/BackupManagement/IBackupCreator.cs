using System;
using System.Threading.Tasks;

namespace Infrastructure.BackupManagement
{
    public interface IBackupCreator
    {
        Task CreateBackupToDestination(string destinationPath);

        Task RestoreBackupToDestination(string sourcePath);
    }
}
