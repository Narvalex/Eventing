using Infrastructure.DateTimeProvider;
using Infrastructure.Logging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.BackupManagement
{
    public class BackupManager : IBackupManager
    {
        private readonly IDateTimeProvider dateTime;
        private readonly IEnumerable<IRelationalDbBackupCreator> relationalDbBackupCreators;
        private DateTime lastBackupDate = new DateTime();
        private readonly ILogLite log = LogManager.GetLoggerFor<BackupManager>();
        private readonly IEventStoreBackupCreator esBackupCreator;
        private readonly string restorePath;
        public bool enabledAutomaticBackups;
        public TimeSpan timeToCreateBackup;
        private readonly Func<string, DateTime, Task>? onBackupCreated;
        public int backupWindowInHours;
        public int maxBackupFiles;
        private readonly string backupDestinationPath;
        private readonly string restoreTempFolder = "temp";
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public BackupManager(IDateTimeProvider dateTime, IEventStoreBackupCreator esBackupCreator, IEnumerable<IRelationalDbBackupCreator> relationalDbBackupCreators,
            string backupDestinationPath, string restorePath, bool enabledAutomaticBackups, TimeSpan timeToCreateBackup, int backupWindowInHours, int maxBackupFiles,
            Func<string, DateTime, Task>? onBackupCreated = null)
        {
            this.dateTime = dateTime.EnsuredNotNull(nameof(dateTime));
            this.relationalDbBackupCreators = relationalDbBackupCreators.EnsuredNotNull(nameof(relationalDbBackupCreators));
            this.esBackupCreator = esBackupCreator.EnsuredNotNull(nameof(esBackupCreator));
            this.enabledAutomaticBackups = enabledAutomaticBackups;
            this.timeToCreateBackup = timeToCreateBackup;
            this.backupWindowInHours = Ensured.NotNegative(backupWindowInHours, nameof(backupWindowInHours));
            this.maxBackupFiles = Ensured.Positive(maxBackupFiles, nameof(maxBackupFiles));
            Ensure.NotEmpty(backupDestinationPath, nameof(backupDestinationPath));
            this.backupDestinationPath = PathParser.GetAbsolutePath(backupDestinationPath);
            this.restorePath = PathParser.GetAbsolutePath(restorePath);
            this.onBackupCreated = onBackupCreated;
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (!this.enabledAutomaticBackups || !cancellationToken.CanBeCanceled) return;

            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var now = this.dateTime.Now;

                    if (now.Date > this.lastBackupDate.Date)
                    {
                        var backupTime = now.Date.Add(this.timeToCreateBackup);
                        var endOfWindowTime = backupTime.AddHours(this.backupWindowInHours);

                        if (now.Between(backupTime, endOfWindowTime))
                        {
                            try
                            {
                                
                                this.CleanOldBackupFiles(); // If there are too much files that was not deleted...
                                await this.CreateBackupFile(now, false, CompressionLevel.Optimal);
                                this.CleanOldBackupFiles(); // To delete de extra file

                                this.lastBackupDate = now.Date;
                            }
                            catch (Exception ex)
                            {
                                this.log.Error(ex, "An error ocurred while creating backups.");
                            }

                        }

                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }

            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        }

        public async Task RestoreSqlDatabases()
        {
            if (!await this.CheckAndExtractZipFilesIfNeccesary())
            {
                this.log.Warning("No sql backup found to restore...");
                return;
            }

            var restoreFilesPath = $"{this.restorePath}\\{this.restoreTempFolder}";
            await Directory
                .EnumerateFiles(restoreFilesPath)
                .Where(x => x.EndsWith(".bak"))
                .ForEachAsync(async filePath =>
                {
                    var virtualName = filePath.Split('\\').Last().Split('.').ToList()
                                        .Transform(x =>
                                        {
                                            x.RemoveAt(x.Count - 1);
                                            return x.Aggregate("", (s, e) => s += e);
                                        });

                    this.log.Info($"Restoring sql db {virtualName}...");
                    await this.relationalDbBackupCreators
                        .First(x => x.VirtualDbName == virtualName)
                        .RestoreBackupToDestination(filePath);
                });

            this.log.Success("Sql databases restored successfully!");
            return;
        }

        public async Task RestoreEventStore()
        {
            if (!await this.CheckAndExtractZipFilesIfNeccesary())
            {
                this.log.Warning("No event store backup found to restore...");
                return;
            }

            await this.esBackupCreator.RestoreBackupToDestination($"{this.restorePath}\\{this.restoreTempFolder}");
            this.log.Success("Event Store restored successfully!");
        }

        public void CleanRestoreFolder()
        {
            this.log.Info($"Cleaning restore folder...");

            var tempPath = $"{this.restorePath}\\{this.restoreTempFolder}";

            if (Directory.Exists($"{tempPath}\\data"))
                Directory.GetFiles($"{tempPath}\\data").ForEach(f => File.SetAttributes(f, FileAttributes.Normal));

            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

            Directory.GetFiles(this.restorePath).ForEach(x => File.Delete(x));

            this.log.Info($"Restore folder cleansed successfully");
        }

        private void CleanOldBackupFiles()
        {
            var backupFiles = Directory.EnumerateFiles(this.backupDestinationPath).Where(x => x.EndsWith(".zip")).ToList();
            if (backupFiles.Count > this.maxBackupFiles)
            {
                this.log.Warning("Cleaning old backup file/s...");
                backupFiles
                    .OrderByDescending(x => x)
                    .Skip(this.maxBackupFiles)
                    .ForEach(x =>
                    {
                        this.log.Warning($"Deleting {x}...");
                        File.Delete(x);
                    });
                this.log.Success("Old backup file/s deleted successfully!");
            }
        }

        /// <remarks>
        /// The order must be preserved.
        /// </remarks>
        private async Task<string> CreateBackupFile(DateTime timeStamp, bool includeTimeInName, CompressionLevel compressionLevel)
        {
            await this.semaphore.WaitAsync();
            try
            {
                var backupName = includeTimeInName
                        ? timeStamp.ToString("yyyy-MM-dd_HH-mm-ss")
                        : timeStamp.Date.ToString("yyyy-MM-dd");

                var newBackupFolderPath = $"{this.backupDestinationPath}\\{backupName}";
                var backupFileName = $"{newBackupFolderPath}.zip";
                var fileNameOnly = backupFileName.Split('\\').Last();
                if (File.Exists(backupFileName))
                {
                    this.log.Success($"Backup file found! A new backup file will be created tomorrow, hopefully. '{fileNameOnly}'");
                    return fileNameOnly;
                }

                var selectedCompression = compressionLevel == CompressionLevel.NoCompression
                                                ? "No compression"
                                                : compressionLevel == CompressionLevel.Fastest
                                                ? "Fastest"
                                                : "Optimal";

                this.log.Warning($"Starting backup file creation with compression level '{selectedCompression}'...");

                if (!Directory.Exists(newBackupFolderPath))
                    Directory.CreateDirectory(newBackupFolderPath);

                IRelationalDbBackupCreator relationalDbBackupCreator;

                relationalDbBackupCreator = this.relationalDbBackupCreators
                                                .Single(x => x.RelationalDbType == RelationalDbType.CheckpointStore);
                if (!File.Exists($"{newBackupFolderPath}\\{relationalDbBackupCreator.VirtualDbName}.bak"))
                {
                    this.log.Info("Creating Checkpoints db backup...");
                    await relationalDbBackupCreator.CreateBackupToDestination(newBackupFolderPath);
                }

                await this.relationalDbBackupCreators
                          .Where(x => x.RelationalDbType == RelationalDbType.ReadModel)
                          .ForEachAsync(async x =>
                          {
                              if (!File.Exists($"{newBackupFolderPath}\\{x.VirtualDbName}.bak"))
                              {
                                  this.log.Info($"Creating {x.VirtualDbName} backup...");
                                  await x.CreateBackupToDestination(newBackupFolderPath);
                              }
                          });

                relationalDbBackupCreator = this.relationalDbBackupCreators
                          .Single(x => x.RelationalDbType == RelationalDbType.SnapshotStore);
                if (!File.Exists($"{newBackupFolderPath}\\{relationalDbBackupCreator.VirtualDbName}.bak"))
                {
                    this.log.Info("Creating Snapshots db backup...");
                    await relationalDbBackupCreator.CreateBackupToDestination(newBackupFolderPath);
                }

                relationalDbBackupCreator = this.relationalDbBackupCreators
                          .Single(x => x.RelationalDbType == RelationalDbType.EventLog);
                if (!File.Exists($"{newBackupFolderPath}\\{relationalDbBackupCreator.VirtualDbName}.bak"))
                {
                    this.log.Info("Creating EventLog db backup...");
                    await relationalDbBackupCreator.CreateBackupToDestination(newBackupFolderPath);
                }

                relationalDbBackupCreator = this.relationalDbBackupCreators
                         .Single(x => x.RelationalDbType == RelationalDbType.Files);
                if (!File.Exists($"{newBackupFolderPath}\\{relationalDbBackupCreator.VirtualDbName}.bak"))
                {
                    this.log.Info("Creating EventStoreDB backup...");
                    await this.esBackupCreator.CreateBackupToDestination(newBackupFolderPath);

                    this.log.Info("Creating Files db backup. Files Db can have orphans but they will be cleansed after adding new files...");
                    await relationalDbBackupCreator.CreateBackupToDestination(newBackupFolderPath);
                }

                this.log.Info($"Compressing backup files...");
                ZipFile.CreateFromDirectory(newBackupFolderPath, backupFileName, compressionLevel, false);

                this.log.Info($"Cleaning unnecessary files...");
                Directory.GetDirectories(this.backupDestinationPath).ForEach(x =>
                {
                    if (Directory.Exists($"{x}\\data"))
                        Directory.GetFiles($"{x}\\data").ForEach(f => File.SetAttributes(f, FileAttributes.Normal));

                    Directory.Delete(x, true);
                });

                this.log.Success($"Backup file created successfully! '{fileNameOnly}'");

                try
                {
                    if (this.onBackupCreated is not null)
                        await this.onBackupCreated(fileNameOnly, timeStamp);
                }
                catch (Exception ex)
                {
                    this.log.Error(ex, $"An error ocurred after the backup was created. This error will be ignored.");
                }

                return fileNameOnly;
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        private async Task<bool> CheckAndExtractZipFilesIfNeccesary()
        {
            if (!Directory.Exists(this.restorePath))
                return false;

            if (Directory.GetDirectories(this.restorePath).Count() > 0)
                return true;

            var file = Directory.GetFiles(this.restorePath).FirstOrDefault();
            if (file.IsEmpty())
                return false;

            this.log.Info("Extracting restore zip file...");
            await Task.Run(() => ZipFile.ExtractToDirectory(file!, $"{this.restorePath}\\{this.restoreTempFolder}"));
            return true;
        }

        public async Task<IResponse<string>> CreateBackup(CompressionLevel compressionLevel)
        {
            var name = await this.CreateBackupFile(this.dateTime.Now, true, compressionLevel);
            return new Response<string>(name);
        }
    }
}
