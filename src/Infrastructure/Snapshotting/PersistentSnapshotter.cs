using Infrastructure.EventSourcing;
using Infrastructure.EventStorage;
using Infrastructure.Logging;
using Infrastructure.Processing;
using Infrastructure.Serialization;
using Infrastructure.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Snapshotting
{
    // NOTE: If an event sourced entity is way too big, it may need to be fully snapshotted in a dev or build machine before deploying in production.
    // HOW TO CACHE: Just activate snapshot for every event and query stuff... this will snapshot everything you are querying
    public class PersistentSnapshotter : IPersistentSnapshotter, IPersistentSnapshotterEngine
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<PersistentSnapshotter>();
        private readonly ConcurrentQueue<SnapshotData> snapshotsQueue = new ConcurrentQueue<SnapshotData>();
        private readonly List<SnapshotSchema> schemas = new List<SnapshotSchema>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task? workerThread;
        private readonly object lockObject = new object();
        private readonly ISnapshotStore snapshotStore;
        private readonly IEventStore eventStore;
        private readonly IJsonSerializer serializer;
        private readonly IExclusiveWriteLock writeLock;
        private readonly TimeSpan idleDelay;
        private readonly int secondsToRestartOnError = 30;
        private readonly int readPageSize;

        // States
        private bool disposedValue;
        private bool workerThreadIsRunning = false;
        private bool isInitialized = false;
        private bool thereAreStaleSnapshots = false;
        private bool snapshotsAreUpdated = false;

        public PersistentSnapshotter(
            ISnapshotStore snapshotStore,
            IEventStore eventStore,
            IJsonSerializer serializer,
            IExclusiveWriteLock writeLock,
            TimeSpan idleDelay,
            int readPageSize = 100)
        {
            this.snapshotStore = snapshotStore.EnsuredNotNull(nameof(snapshotStore));
            this.eventStore = eventStore.EnsuredNotNull(nameof(eventStore));
            this.serializer = serializer.EnsuredNotNull(nameof(serializer));

            Ensure.Positive(idleDelay.TotalMilliseconds, nameof(idleDelay));
            this.writeLock = writeLock.EnsuredNotNull(nameof(writeLock));
            this.idleDelay = idleDelay;

            this.readPageSize = Ensured.Positive(readPageSize, nameof(readPageSize));
        }

        public void EnqueueWrite(SnapshotData snapshotData)
        {
            this.StartEngineIfNecessary();
            this.snapshotsQueue.Enqueue(snapshotData);
        }

        public async Task<T?> TryGet<T>(string streamName) where T : class, IEventSourced
        {
            this.StartEngineIfNecessary();

            while (!this.isInitialized)
            {
                await Task.Delay(100);
            }

            var schema = this.schemas.FirstOrDefault(x => x.Type == typeof(T).FullName);
            if (schema is null)
                return null;

            var snapshotData = await this.snapshotStore.TryGetSnapshot(streamName, schema.Version);
            if (snapshotData is null)
                return null;

            var snapshot = this.serializer.Deserialize<T>(snapshotData.Payload);
            return snapshot;
        }

        public async Task<IEventSourced?> TryGet(Type type, string streamName)
        {
            this.StartEngineIfNecessary();

            while (!this.isInitialized)
            {
                await Task.Delay(100);
            }

            var schema = this.schemas.FirstOrDefault(x => x.Type == type.FullName);
            if (schema is null)
                return null;

            var snapshotData = await this.snapshotStore.TryGetSnapshot(streamName, schema.Version);
            if (snapshotData is null)
                return null;

            var snapshot = this.serializer.Deserialize<IEventSourced>(snapshotData.Payload);
            return snapshot;
        }

        public IPersistentSnapshotterEngine StartEngineIfNecessary()
        {
            if (this.workerThreadIsRunning) return this;
            lock (this.lockObject)
            {
                if (this.workerThreadIsRunning || this.cancellationTokenSource.IsCancellationRequested) return this;

                this.workerThread = Task
                    .Factory
                    .StartNew(() => 
                        this.RunWorkerThreadUntilCancellation(this.cancellationTokenSource.Token), 
                        this.cancellationTokenSource.Token, 
                        TaskCreationOptions.LongRunning, 
                        TaskScheduler.Current
                    );

                this.workerThreadIsRunning = true;
            }
            return this;
        }

        public async Task WaitUntilSnapshotsAreUpdated()
        {
            while (!this.snapshotsAreUpdated)
            {
                await Task.Delay(100);
            }

            return;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    using (this.workerThread)
                    using (this.cancellationTokenSource)
                    {
                        this.cancellationTokenSource.Cancel();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.snapshotsQueue.Clear();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private async Task RunWorkerThreadUntilCancellation(CancellationToken token)
        {
            // TODO: Handle all failure cases here
            try
            {
                await this.InitializeSchemas();
            }
            catch (Exception ex)
            {
                this.log.Fatal(ex, "Failed at schemas initialization");
                throw;
            }

            do
            {
                try
                {
                    if (this.thereAreStaleSnapshots)
                    {
                        this.thereAreStaleSnapshots = await this.UpdateAStaleSnapshotAndCheckIfThereAreRemainingStaleSnapshots();
                    }

                    if (!this.thereAreStaleSnapshots && !await this.TryPersistNewSnapshotsFromQueue())
                        await Task.Delay(this.idleDelay);
                }
                catch (Exception ex)
                {
                    this.log.Error(ex, $"An error ocurred. The worker will restart in {this.secondsToRestartOnError} seconds");
                    await Task.Delay(TimeSpan.FromSeconds(this.secondsToRestartOnError));
                }

            } while (!token.IsCancellationRequested);
        }

        private async Task InitializeSchemas()
        {
            this.log.Info("Acquiring write lock...");
            await this.writeLock.WaitLockAcquisition(this.cancellationTokenSource.Token);
            this.log.Info("Lock acquired. Initializing snapshot schemas...");
            var dbSchemas = await this.snapshotStore.GetSchemas();

            var pendingUpdates = new List<SnapshotSchema>();
            foreach (var dbSchema in dbSchemas)
            {
                var snapshotType = dbSchema.GetSnapshotType();
                if (snapshotType == null)
                    throw new InvalidOperationException($"The eventsourced/aggregate type of {dbSchema.Type} does not exist in assembly/dll of {dbSchema.Assembly}");

                var freshSchemaHash = EventSourcedEntityHasher.GetHash(snapshotType);
                if (!dbSchema.Hash.IsEqualWithOrdinalIgnoreCaseComparisson(freshSchemaHash))
                {
                    this.log.Warning($"The schema for snapshot of type {dbSchema.Type} has changed. All persisted snapshots of this type needs to be rebuilt. This might take time.");

                    pendingUpdates.Add(dbSchema.UpdateSchema(freshSchemaHash));
                }
            }

            if (pendingUpdates.Count > 0)
                await this.snapshotStore.Save(pendingUpdates.ToArray());

            this.schemas.AddRange(dbSchemas);
            this.thereAreStaleSnapshots = dbSchemas.Any(x => x.ThereAreStaleSnapshots);

            this.isInitialized = true;
            this.log.Success(this.thereAreStaleSnapshots
                ? "Snapshot schemas were successfully initialized. There are stale snapshots."
                : "Snapshot schemas were successfully initialized. All snapshots are up to date.");
            if (!this.thereAreStaleSnapshots)
                this.snapshotsAreUpdated = true;
        }

        // This method is called in a loop to find if there are any stale snapshot
        private async Task<bool> UpdateAStaleSnapshotAndCheckIfThereAreRemainingStaleSnapshots()
        {
            var schemaWithStaleSnapshots = this.schemas.FirstOrDefault(x => x.ThereAreStaleSnapshots);
            if (schemaWithStaleSnapshots is null)
            {
                this.log.Success("All snapshots where updated and are now up to date");
                this.snapshotsAreUpdated = true;
                return false;
            }

            var snapshotData = await this.snapshotStore.TryGetFirstStaleSnapshot(schemaWithStaleSnapshots.Type, schemaWithStaleSnapshots.Version);
            if (snapshotData is null)
            {
                await this.snapshotStore
                          .Save(schemaWithStaleSnapshots.NotifyAllSnapshotsAreUpToDate());
            }
            else
            {
                await this.UpdateSnapshot(snapshotData, schemaWithStaleSnapshots);
            }

            return true;
        }

        private async Task<bool> TryPersistNewSnapshotsFromQueue()
        {
            if (this.snapshotsQueue.IsEmpty)
                return false;

            var dictionary = new Dictionary<string, SnapshotData>();
            while (this.snapshotsQueue.TryDequeue(out var snapshotData))
            {
                // If system is too busy this will be an endless loop
                snapshotData.SchemaVersion = await this.ResolvePotentiallyBrandNewVersion(snapshotData);
                dictionary[snapshotData.StreamName] = snapshotData;
            }

            try
            {
                await this.snapshotStore.Save(dictionary.Values.ToArray());
            }
            catch (Exception ex)
            {
                this.log.Error(ex, "Error on new snapshot persistence. This error will be ignored.");
                return false;
            }

            return true;
        }

        private async Task UpdateSnapshot(SnapshotData snapshotData, SnapshotSchema schema)
        {
            // Update to store, from event store
            var snapshot = (IEventSourced)EventSourcedCreator.New(schema.GetSnapshotType());

            await snapshot.TryRehydrate(snapshotData.StreamName, snapshotData.Version, this.eventStore, 0, this.readPageSize);

            var payload = this.serializer.Serialize(snapshot);

            var bytes = Encoding.UTF8.GetBytes(payload);

            await this.snapshotStore.Save(
                new SnapshotData(snapshotData.StreamName, snapshot.Metadata.Version, payload, snapshot.GetEntityType(), snapshot.GetAssembly(), bytes.LongLength, schema.Version));
        }

        private async Task<int> ResolvePotentiallyBrandNewVersion(SnapshotData snapshotData)
        {
            var schema = this.schemas.FirstOrDefault(s => s.Type == snapshotData.Type);
            if (schema != null)
                return schema.Version;

            var brandNewVersion = 0;
            var hash = EventSourcedEntityHasher.GetHash(snapshotData.GetSnapshotType());
            var newSchema = new SnapshotSchema(snapshotData.Type, snapshotData.Assembly, brandNewVersion, hash, false);
            await this.snapshotStore.Save(newSchema);
            this.schemas.Add(newSchema);
            return brandNewVersion;
        }
    }
}
