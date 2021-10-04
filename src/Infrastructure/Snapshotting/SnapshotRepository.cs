using Infrastructure.EventSourcing;
using Infrastructure.Logging;
using Infrastructure.Processing;
using Infrastructure.Serialization;
using Infrastructure.Utils;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Snapshotting
{
    public class SnapshotRepository : ISnapshotRepository, IPersistentSnapshotterEngine
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<SnapshotRepository>();

        private readonly MemoryCache cache;
        private readonly TimeSpan timeToLive;
        private readonly IExclusiveWriteLock writeLock;
        private readonly IJsonSerializer serializer;
        private readonly int interval;
        private readonly int doubleInterval;
        private readonly IPersistentSnapshotter persistentSnapshotter;
        private readonly SnapshotEvictionLogger evictionLogger;
        private bool shouldRegisterEvictionCallback;

        public SnapshotRepository(IExclusiveWriteLock writeLock, IJsonSerializer serializer, IPersistentSnapshotter persistentSnapshotter, TimeSpan timeToLive, int interval = 10, long cacheSizeLimitInBytes = 256_000_000)
        {
            Ensure.NotNull(serializer, nameof(serializer));
            Ensure.Positive(timeToLive.TotalMilliseconds, nameof(timeToLive));
            Ensure.Positive(cacheSizeLimitInBytes, nameof(cacheSizeLimitInBytes));
            this.persistentSnapshotter = persistentSnapshotter.EnsuredNotNull(nameof(persistentSnapshotter));

            // Compaction ratio: https://developpaper.com/memorycache-caching-options-in-the-net-core-series/
            // Defaults to 0.05 (5%)
            this.cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = cacheSizeLimitInBytes });

            this.timeToLive = timeToLive;
            this.writeLock = writeLock.EnsuredNotNull(nameof(writeLock));
            this.serializer = serializer;
            this.interval = Ensured.Positive(interval, nameof(interval));
            this.doubleInterval = this.interval * 2;
            this.evictionLogger = new SnapshotEvictionLogger(this.log);
            this.shouldRegisterEvictionCallback = this.log.VerboseEnabled;
        }

        public void Save(IEventSourced snapshot)
        {
            if (!this.writeLock.IsAcquired) return;

            // CQRS Journey project quote:
            // make a copy of the state values to avoid concurrency problems with reusing references.

            // The next Stackoverflow link looks like encourages to use ReadOnlyMemory<byte>
            // link: https://stackoverflow.com/questions/61374796/c-sharp-convert-readonlymemorybyte-to-byte

            var payload = this.serializer.Serialize(snapshot);
            var bytes = Encoding.UTF8.GetBytes(payload);
            var cacheValue = new ReadOnlyMemory<byte>(bytes);
            var size = bytes.LongLength;

            var options = new MemoryCacheEntryOptions()
                    .SetSize(size)
                    .SetPriority(this.GetPriority(snapshot))
                    .SetAbsoluteExpiration(DateTimeOffset.UtcNow.Add(this.timeToLive));

            if (this.shouldRegisterEvictionCallback)
                options.RegisterPostEvictionCallback(this.evictionLogger.CacheEvictionCallback);

            this.cache.Set(
                key: snapshot.Metadata.StreamName,
                value: cacheValue,
                options: options
            );

            // Try persist snapshot
            var version = snapshot.Metadata.Version;
            if ((version % this.interval) != 0 || (version == 0 && this.interval > 1))
                return;

            this.persistentSnapshotter
                .EnqueueWrite(new SnapshotData(snapshot.Metadata.StreamName, snapshot.Metadata.Version, payload, snapshot.GetEntityType(), snapshot.GetAssembly(), size));
        }

        public void SaveIfNotExists(IEventSourced snapshot)
        {
            if (this.cache.TryGetValue(snapshot.Metadata.StreamName, out _))
                return;

            this.Save(snapshot);
        }

        public bool TryGetFromMemory<T>(string streamName, out T? snapshot) where T : class, IEventSourced
        {
            if (this.cache.TryGetValue<ReadOnlyMemory<byte>>(streamName, out var serialized))
            {
                snapshot = this.serializer.Deserialize<T>(
                    Encoding.UTF8.GetString(serialized.Span)
                );

                return true;
            }
            else
            {
                snapshot = default;
                return false;
            }
        }

        public bool TryGetFromMemory(Type type, string streamName, out IEventSourced? snapshot)
        {
            if (this.cache.TryGetValue<ReadOnlyMemory<byte>>(streamName, out var serialized))
            {
                snapshot = (IEventSourced)this.serializer.Deserialize(
                    Encoding.UTF8.GetString(serialized.Span),
                    type
                )!;

                return true;
            }
            else
            {
                snapshot = default;
                return false;
            }
        }

        public bool ExistsInMemory(string streamName) =>
            this.cache.TryGetValue(streamName, out _);

        public Task<T?> TryGetFromPersistentRepository<T>(string streamName) where T : class, IEventSourced =>
            this.persistentSnapshotter.TryGet<T>(streamName);

        public Task<IEventSourced?> TryGetFromPersistentRepository(Type type, string streamName) =>
            this.persistentSnapshotter.TryGet(type, streamName);

        public void InvalidateInMemorySnapshot(string streamName)
        {
            this.cache.Remove(streamName);
        }

        public IPersistentSnapshotterEngine StartEngineIfNecessary() => ((IPersistentSnapshotterEngine)this.persistentSnapshotter).StartEngineIfNecessary();

        public Task WaitUntilSnapshotsAreUpdated() => ((IPersistentSnapshotterEngine)this.persistentSnapshotter).WaitUntilSnapshotsAreUpdated();

        private CacheItemPriority GetPriority(IEventSourced snapshot)
        {
            var version = snapshot.Metadata.Version;
            if (version < this.interval)
                return CacheItemPriority.Low;
            if (version < this.doubleInterval)
                return CacheItemPriority.Normal;

            return CacheItemPriority.High;
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    using (this.cache)
                    using (this.persistentSnapshotter as IPersistentSnapshotterEngine)
                    {
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
