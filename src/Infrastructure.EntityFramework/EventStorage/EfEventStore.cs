using Infrastructure.DateTimeProvider;
using Infrastructure.EntityFramework.EventStorage.Database;
using Infrastructure.EventSourcing;
using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.EventStorage
{
    public class EfEventStore : IEventStore
    {
        private readonly IJsonSerializer serializer;
        private readonly Func<EventStoreDbContext> writeContextFactory;
        private readonly Func<EventStoreDbContext> readContextFactory;
        private readonly IDateTimeProvider dateTime;
        private readonly IEventDeserializationAndVersionManager eventVersionManager;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private long lastPosition = -1;

        public EfEventStore(Func<EventStoreDbContext> contextFactory, IJsonSerializer serializer, IEventDeserializationAndVersionManager eventDeserializer, IDateTimeProvider dateTime)
            : this(contextFactory, contextFactory, serializer, eventDeserializer, dateTime)
        { }

        public EfEventStore(Func<EventStoreDbContext> writeContextFactory, Func<EventStoreDbContext> readContextFactory, IJsonSerializer serializer, IEventDeserializationAndVersionManager eventUpcastingManager, IDateTimeProvider dateTime)
        {
            this.writeContextFactory = Ensured.NotNull(writeContextFactory, nameof(writeContextFactory));
            this.readContextFactory = Ensured.NotNull(readContextFactory, nameof(readContextFactory));
            this.serializer = Ensured.NotNull(serializer, nameof(serializer));
            this.dateTime = Ensured.NotNull(dateTime, nameof(dateTime));

            using (var context = this.readContextFactory())
            {
                var databaseCreator = context.GetService<IDatabaseCreator>();
                if (databaseCreator is RelationalDatabaseCreator)
                {
                    // Sql database
                    if ((databaseCreator as RelationalDatabaseCreator).Exists())
                        if (context.Events.Any())
                            this.lastPosition = context.Events.Max(x => x.Position);
                }
                else
                {
                    // In memory database
                    if (!databaseCreator.EnsureCreated())
                        if (context.Events.Any())
                            this.lastPosition = context.Events.Max(x => x.Position);
                }
            }

            this.eventVersionManager = Ensured.NotNull(eventUpcastingManager, nameof(eventUpcastingManager));
        }

        public IEventDeserializationAndVersionManager EventVersionManager => this.eventVersionManager;

        public async Task AppendToStreamAsync(string streamName, IEnumerable<IEvent> events)
        {
            await this.DoAppend(streamName, events);
        }

        public async Task AppendToStreamAsync(string streamName, long expectedVersion, IEnumerable<IEvent> events)
        {
            await this.DoAppend(streamName, events, true, expectedVersion);
        }

        public async Task<bool> CheckStreamExistenceAsync(string streamName)
        {
            using (var context = this.readContextFactory())
            {
                var category = EventStream.GetCategory(streamName);
                var sourceId = EventStream.GetId(streamName);
                return await context.Events.AnyAsync(x => x.Category == category && x.SourceId == sourceId);
            }
        }

        public async Task<string> ReadLastStreamFromCategory(string category, int offset = 0)
        {
            using (var context = this.readContextFactory())
            {
                var entity = await context.Events
                        .Where(x => x.Category == category)
                        .OrderByDescending(x => x.SourceId)
                        .Skip(offset)
                        .FirstOrDefaultAsync();

                return entity?.SourceId;
            }
        }

        public async Task<EventStreamSlice> ReadStreamForwardAsync(string streamName, long from, int count)
        {
            using (var context = this.readContextFactory())
            {
                var category = EventStream.GetCategory(streamName);
                var sourceId = EventStream.GetId(streamName);
                var exists = await context.Events.AnyAsync(x => x.Category == category && x.SourceId == sourceId);
                var end = (from + count) - 1;

                if (!exists)
                    return new EventStreamSlice(SliceFetchStatus.StreamNotFound, null, 0, true);


                var descriptors = await context.Events
                                    .Where(x => x.Category == category
                                                && x.SourceId == sourceId
                                                && x.Version >= from
                                                && x.Version <= end)
                                    .OrderBy(x => x.Version)
                                    .ToArrayAsync();

                var events = descriptors
                                .Select(x =>
                                    this.eventVersionManager
                                        .GetLatestEventVersion(
                                            x.EventType,
                                            x.Version,
                                            x.Version,
                                            x.Payload,
                                            x.Metadata,
                                            x.Category))
                                .ToArray();

                long lastFoundVersion;
                bool isEndOfStream;
                if (events.Length == 0)
                {
                    lastFoundVersion = -1;
                    isEndOfStream = true;
                }
                else
                {
                    lastFoundVersion = descriptors.Last().Version;
                    isEndOfStream = !await context.Events
                                        .AnyAsync(x => x.Category == category
                                                       && x.SourceId == sourceId
                                                       && x.Version > lastFoundVersion);
                }
                var nextEventNumber = lastFoundVersion + 1;

                return new EventStreamSlice(SliceFetchStatus.Success, events, nextEventNumber, isEndOfStream);
            }
        }

        public async Task<CategoryStreamsSlice> ReadStreamsFromCategoryAsync(string category, long from, int count)
        {
            using (var context = this.readContextFactory())
            {
                var skipped = (int)from - 1;
                if (skipped < 0)
                    skipped = 0;

                if (!await context.Events.AnyAsync(x => x.Category == category && x.Version == 0))
                    return new CategoryStreamsSlice(SliceFetchStatus.StreamNotFound, null, 0, true);

                var sourceIds = await context
                                        .Events
                                        .Where(x => x.Category == category && x.Version == 0)
                                        .OrderBy(x => x.Position)
                                        .Skip(skipped)
                                        .Take(count + 1)
                                        .Select(x => x.SourceId)
                                        .ToArrayAsync();

                var endOfStream = sourceIds.Count() < count + 1;

                // we clean the sourceIds with the +1 only fetched to know if it is the end.
                sourceIds = endOfStream ? sourceIds.ToArray() : sourceIds.SkipLast(1).ToArray();

                return new CategoryStreamsSlice(
                    SliceFetchStatus.Success,
                    await sourceIds
                            .SelectAsync(async x => 
                                new StreamNameAndVersion(
                                    $"{category}-{x}", EventStream.NoEventsNumber))
                            .ToListAsync(),
                    (int)from + sourceIds.Length,
                    endOfStream);
            }
        }

        public async Task<CategoryStreamsSlice> ReadStreamsFromCategoryAsync(string category, long from, int count, long maxEventNumber)
        {
            using (var context = this.readContextFactory())
            {
                var skipped = (int)from - 1;
                if (skipped < 0)
                    skipped = 0;

                if (!await context.Events.AnyAsync(x => x.Category == category && x.Version == 0 && x.Position <= maxEventNumber))
                    return new CategoryStreamsSlice(SliceFetchStatus.StreamNotFound, null, 0, true);

                var sourceIds = await context
                                        .Events
                                        .Where(x => x.Category == category && x.Version == 0 && x.Position <= maxEventNumber)
                                        .OrderBy(x => x.Position)
                                        .Skip(skipped)
                                        .Take(count + 1)
                                        .Select(x => x.SourceId)
                                        .ToArrayAsync();

                var endOfStream = sourceIds.Count() < count + 1;

                // we clean the sourceIds with the +1 only fetched to know if it is the end.
                sourceIds = endOfStream ? sourceIds.ToArray() : sourceIds.SkipLast(1).ToArray();

                return new CategoryStreamsSlice(
                    SliceFetchStatus.Success,
                    await sourceIds
                            .SelectAsync(async x =>
                                new StreamNameAndVersion(
                                    $"{category}-{x}",
                                    await context.Events.Where(y => y.Category == category && y.SourceId == x && y.Position <= maxEventNumber)
                                    .Select(y => y.Version)
                                    .MaxAsync()))
                            .ToListAsync(),
                    (int)from + sourceIds.Length,
                    endOfStream);
            }
        }

        private async Task DoAppend(string streamName, IEnumerable<IEvent> events, bool checkExpectedVersion = false, long expectedVersion = -2)
        {
            using (var context = this.writeContextFactory())
            {
                var firstEvent = events.First();
                var sourceCategory = EventStream.GetCategory(streamName);
                var timestamp = this.dateTime.Now;

                // Pesismistic concurrency
                await this.semaphore.WaitAsync();
                try
                {
                    var lastEvent = context.Events
                                    .Where(x => x.Category == sourceCategory
                                                && x.SourceId == firstEvent.StreamId)
                                    .OrderByDescending(x => x.Version)
                                    .FirstOrDefault();

                    long currentVersion = lastEvent is null ? -1 : lastEvent.Version;
                    if (checkExpectedVersion && expectedVersion != currentVersion)
                        throw new OptimisticConcurrencyException($"The expected version was {expectedVersion} but current version is {currentVersion}");

                    var currentPosition = this.lastPosition;

                    foreach (var e in events)
                    {
                        currentPosition++;
                        currentVersion++;
                        var entity = new EventDescriptor
                        {
                            Position = currentPosition,
                            Category = sourceCategory,
                            SourceId = firstEvent.StreamId,
                            Version = currentVersion,
                            EventType = e.GetType().Name.WithFirstCharInLower(),
                            TimeStamp = timestamp,
                            Payload = this.serializer.Serialize(e),
                            Metadata = this.serializer.SerializeDictionary(e.GetEventMetadata().ToDictionary())
                        };

                        context.Events.Add(entity);
                    }

                    await context.SaveChangesAsync();

                    this.lastPosition = currentPosition;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    this.semaphore.Release();
                }
            }
        }
    }
}
