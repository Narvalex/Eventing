using Infrastructure.EventLog;
using Infrastructure.EventSourcing;
using Infrastructure.EventStorage;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework.EventLog
{
    public class EfEventLogReader : IEventLogReader,
        IWaitForEventLogToBeConsistent // This is for query tools and Migration tools, nor for app in production
    {
        private readonly Func<EventLogDbContext> contextFactory;

        public EfEventLogReader(Func<EventLogDbContext> contextFactory)
        {
            this.contextFactory = contextFactory.EnsuredNotNull(nameof(contextFactory));
        }

        public async Task<CategoryStreamsSlice> GetCategoryStreamsSliceAsync(string category, long from, int count)
        {
            using (var context = this.contextFactory())
            {
                var skipped = (int)from - 1;
                if (skipped < 0)
                    skipped = 0;

                if (!await context.Events.AnyAsync(x => x.EventSourcedType == category && x.EventSourcedVersion == 0))
                    return new CategoryStreamsSlice(SliceFetchStatus.StreamNotFound, null, 0, true);

                var sourceIds = await context
                                        .Events
                                        .Where(x => x.EventSourcedType == category && x.EventSourcedVersion == 0)
                                        .OrderBy(x => x.EventNumber)
                                        .Skip(skipped)
                                        .Take(count + 1)
                                        .Select(x => x.StreamId)
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

        public async Task<CategoryStreamsSlice> GetCategoryStreamsSliceAsync(string category, long from, int count, long maxEventNumber)
        {
            using (var context = this.contextFactory())
            {
                var skipped = (int)from - 1;
                if (skipped < 0)
                    skipped = 0;

                if (!await context.Events.AnyAsync(x => x.EventSourcedType == category && x.EventSourcedVersion == 0))
                    return new CategoryStreamsSlice(SliceFetchStatus.StreamNotFound, null, 0, true);

                var sourceIds = await context
                                        .Events
                                        .Where(x => x.EventSourcedType == category && x.EventSourcedVersion == 0 && x.EventNumber <= maxEventNumber)
                                        .OrderBy(x => x.EventNumber)
                                        .Skip(skipped)
                                        .Take(count + 1)
                                        .Select(x => x.StreamId)
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
                                    await context.Events.Where(y => y.EventSourcedType == category && y.StreamId == x && y.EventNumber <= maxEventNumber)
                                    .Select(y => y.EventSourcedVersion).MaxAsync()))
                            .ToListAsync(),
                    (int)from + sourceIds.Length,
                    endOfStream);
            }
        }

        public async Task<string?> ReadLastStreamFromCategory(string category, int offset = 0)
        {
            using (var context = this.contextFactory())
            {
                var entity = await context.Events
                        .Where(x => x.EventSourcedType == category && x.EventSourcedVersion == 0)
                        .OrderByDescending(x => x.EventNumber) // this is important because is hard to cast all streamsId to number
                        .Skip(offset)
                        .FirstOrDefaultAsync();

                return entity?.StreamId;
            }
        }

        public async Task WaitForEventLogToBeConsistentToCommitPosition(long commitPosition)
        {
            using (var context = this.contextFactory())
            {
                await TaskRetryFactory.StartPollingAsync(
                    async () => await context.Events.OrderByDescending(x => x.EventNumber).FirstOrDefaultAsync(),
                    e => e?.CommitPosition >= commitPosition,
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromSeconds(120));
            }
        }

        public Task WaitForEventLogToBeConsistentToEventNumber(long eventNumber)
        {
            using (var context = this.contextFactory())
            {
                return TaskRetryFactory.StartPollingAsync(
                    () => context.Events.LastOrDefaultAsync(),
                    e => e.EventNumber >= eventNumber,
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromSeconds(120));
            }
        }
    }
}
