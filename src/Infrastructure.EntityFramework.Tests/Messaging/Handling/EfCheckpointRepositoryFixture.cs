using Infrastructure.DateTimeProvider;
using Infrastructure.EntityFramework.Messaging.Handling;
using Infrastructure.EntityFramework.Messaging.Handling.Database;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.EntityFramework.Tests.Messaging.Handling
{
    public abstract class given_empty_database
    {
        protected Func<CheckpointStoreDbContext> contextFactory;
        protected string dbName = Guid.NewGuid().ToString();

        public given_empty_database()
        {
            this.contextFactory = () => CheckpointStoreDbContext.ResolveInMemoryContext(this.dbName);
        }
    }

    public class given_emtpty_checkpoint_repository : given_empty_database, IDisposable
    {
        private EfCheckpointStore sut;

        public given_emtpty_checkpoint_repository()
        {
            this.sut = new EfCheckpointStore(this.contextFactory, this.contextFactory, new LocalDateTimeProvider(), TimeSpan.Zero, TimeSpan.Zero);
        }

        public void Dispose()
        {
            this.sut?.Dispose();
        }

        [Fact]
        public void then_fetching_checkpoint_then_returns_no_stream_result()
        {
            var checkpoint = this.sut.GetCheckpoint(new EventProcessorId("ElasticSearch", EventProcessorConsts.ReadModelProjection));
            Assert.Equal(Checkpoint.Start, checkpoint);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(580)]
        public void when_updating_checkpoint_then_can_fetch_again(long checkpoint)
        {
            var c = new Checkpoint(new EventPosition(checkpoint, checkpoint), checkpoint);

            var subName = new EventProcessorId("MongoDb", EventProcessorConsts.ReadModelProjection);
            this.sut.CreateOrUpdate(subName, c);
            var retrieved = this.sut.GetCheckpoint(subName);

            Assert.Equal(c, retrieved);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(580)]
        public async Task when_updating_checkpoint_then_is_persisted_in_database(long checkpoint)
        {
            var subName = new EventProcessorId("MongoDb", EventProcessorConsts.EventHandler);
            using (var context = this.contextFactory())
            {
                Assert.False(await context.Checkpoints.AnyAsync(x => x.SubscriptionId == subName.SubscriptionName));
            }

            var c = new Checkpoint(new EventPosition(checkpoint, checkpoint), checkpoint);

            this.sut.CreateOrUpdate(subName, c);

            using (var context = this.contextFactory())
            {
                await TaskRetryFactory.StartPollingAsync(
                    async () => await context.Checkpoints.AnyAsync(x => x.SubscriptionId == subName.SubscriptionName),
                    x => x,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(10));

                var entity = await context.Checkpoints.FirstOrDefaultAsync(x => x.SubscriptionId == subName.SubscriptionName);
                Assert.Equal(subName.SubscriptionName, entity.SubscriptionId);
                Assert.Equal(c.EventNumber, entity.EventNumber);
                Assert.Equal(c.EventPosition.CommitPosition, entity.CommitPosition);
                Assert.Equal(c.EventPosition.PreparePosition, entity.PreparePosition);
                Assert.True(entity.TimeStamp != default(DateTime));
            }
        }
    }

    public class when_updating_burst_of_checkpoints : given_empty_database, IDisposable
    {
        private EfCheckpointStore sut;

        private KeyValuePair<EventProcessorId, long>[] burstList;

        public when_updating_burst_of_checkpoints()
        {
            this.burstList = new KeyValuePair<EventProcessorId, long>[]
            {
                new KeyValuePair<EventProcessorId, long>(new EventProcessorId("stream1", EventProcessorConsts.EventHandler), 0),
                new KeyValuePair<EventProcessorId, long>(new EventProcessorId("stream2", EventProcessorConsts.EventHandler), 0),
                new KeyValuePair<EventProcessorId, long>(new EventProcessorId("stream1", EventProcessorConsts.EventHandler), 1),
                new KeyValuePair<EventProcessorId, long>(new EventProcessorId("stream2", EventProcessorConsts.EventHandler), 1),
                new KeyValuePair<EventProcessorId, long>(new EventProcessorId("stream1", EventProcessorConsts.EventHandler), 2),
                new KeyValuePair<EventProcessorId, long>(new EventProcessorId("stream2", EventProcessorConsts.EventHandler), 2),

            };

            this.sut = new EfCheckpointStore(this.contextFactory, this.contextFactory, new LocalDateTimeProvider(), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            this.burstList.ForEach(x =>
            {
                this.sut.CreateOrUpdate(x.Key, new Checkpoint(new EventPosition(x.Value, x.Value), x.Value));
            });
        }

        public void Dispose()
        {
            this.sut?.Dispose();
        }

        [Theory]
        [InlineData("stream1")]
        [InlineData("stream2")]
        public async Task then_updates_in_db_only_the_lastest_one(string subName)
        {
            using (var context = this.contextFactory())
            {
                await TaskRetryFactory.StartPollingAsync(
                    async () => await context.Checkpoints.AnyAsync(x => x.SubscriptionId == subName),
                    x => x,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(10));

                var entity = await context.Checkpoints.FirstOrDefaultAsync(x => x.SubscriptionId == subName);
                Assert.Equal(2, entity.EventNumber);
            }
        }

        [Fact]
        public void then_can_dispose_right_away()
        {
            this.sut.Dispose();
        }
    }
}
