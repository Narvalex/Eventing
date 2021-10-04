using Infrastructure.BackupManagement;
using Infrastructure.Logging;
using Infrastructure.Utils;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.EventStore
{
    public class EventStoreBackupCreator : IEventStoreBackupCreator
    {
        private readonly string originPath;
        private ILogLite log = LogManager.GetLoggerFor<EventStoreBackupCreator>();

        public EventStoreBackupCreator(string originPath)
        {
            this.originPath = Ensured.NotEmpty(originPath, nameof(originPath));
        }

        public Task CreateBackupToDestination(string destinationPath)
        {
            Ensure.NotEmpty(destinationPath, nameof(destinationPath));
            destinationPath = $"{destinationPath}\\data";

            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            // Index section
            if (!Directory.Exists($"{originPath}\\index"))
                return Task.CompletedTask;

            if (!Directory.Exists($"{destinationPath}\\index"))
                Directory.CreateDirectory($"{destinationPath}\\index");

            this.log.Info("Copying index files...");
            foreach (var indexFile in Directory.EnumerateFiles($"{originPath}\\index").Select(x => x.Split('\\').Last()))
            {
                this.log.Info($"Copying index file {indexFile}...");
                File.Copy($"{originPath}\\index\\{indexFile}", $"{destinationPath}\\index\\{indexFile}", true);
            }

            // Chk section
            this.log.Info("Copying chk files...");
            File.Copy($"{this.originPath}\\chaser.chk", $"{destinationPath}\\chaser.chk", true);
            File.Copy($"{this.originPath}\\epoch.chk", $"{destinationPath}\\epoch.chk", true);
            File.Copy($"{this.originPath}\\proposal.chk", $"{destinationPath}\\proposal.chk", true);
            File.Copy($"{this.originPath}\\writer.chk", $"{destinationPath}\\writer.chk", true);

            this.log.Info("Creating truncate.chk from chaser.chk...");
            File.Copy($"{destinationPath}\\chaser.chk", $"{destinationPath}\\truncate.chk", true);

            // Chunks section
            this.log.Info("Copying chunk files...");
            var chunkFiles = Directory
                            .EnumerateFiles(this.originPath)
                            .Select(x => x.Split('\\').Last())
                            .Where(x => x.StartsWith("chunk"));

            foreach (var chunkFile in chunkFiles)
            {
                this.log.Info($"Copying {chunkFile}...");
                File.Copy($"{this.originPath}\\{chunkFile}", $"{destinationPath}\\{chunkFile}", true);
            }

            return Task.CompletedTask;
        }

        public Task RestoreBackupToDestination(string sourcePath)
        {
            if (!Directory.Exists(this.originPath))
                Directory.CreateDirectory(this.originPath);

            sourcePath = $"{sourcePath}\\data";

            this.log.Info("Copying chk files...");
            File.Copy($"{sourcePath}\\chaser.chk", $"{this.originPath}\\chaser.chk", true);
            File.Copy($"{sourcePath}\\epoch.chk", $"{this.originPath}\\epoch.chk", true);
            File.Copy($"{sourcePath}\\proposal.chk", $"{this.originPath}\\proposal.chk", true);
            File.Copy($"{sourcePath}\\writer.chk", $"{this.originPath}\\writer.chk", true);
            File.Copy($"{sourcePath}\\truncate.chk", $"{this.originPath}\\truncate.chk", true);

            this.log.Info("Copying chunk files...");
            var chunkFiles = Directory
                            .EnumerateFiles(sourcePath)
                            .Select(x => x.Split('\\').Last())
                            .Where(x => x.StartsWith("chunk"));

            foreach (var chunkFile in chunkFiles)
            {
                this.log.Info($"Copying {chunkFile}...");
                if (File.Exists($"{this.originPath}\\{chunkFile}"))
                    File.SetAttributes($"{this.originPath}\\{chunkFile}", FileAttributes.Normal);

                File.Copy($"{sourcePath}\\{chunkFile}", $"{this.originPath}\\{chunkFile}", true);
            }

            if (!Directory.Exists($"{sourcePath}\\index"))
                return Task.CompletedTask;

            if (!Directory.Exists($"{this.originPath}\\index"))
                Directory.CreateDirectory($"{this.originPath}\\index");

            this.log.Info("Copying index files...");
            foreach (var indexFile in Directory.EnumerateFiles($"{sourcePath}\\index").Select(x => x.Split('\\').Last()))
            {
                this.log.Info($"Copying index file {indexFile}...");
                File.Copy($"{sourcePath}\\index\\{indexFile}", $"{this.originPath}\\index\\{indexFile}", true);
            }

            return Task.CompletedTask;
        }
    }
}
