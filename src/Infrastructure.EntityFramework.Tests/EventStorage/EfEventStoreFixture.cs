using Infrastructure.DateTimeProvider;
using Infrastructure.EntityFramework.EventStorage;
using Infrastructure.EntityFramework.EventStorage.Database;
using Infrastructure.EntityFramework.Tests.EventStorage.Helpers;
using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.EntityFramework.Tests.EventStorage
{
    public abstract class given_empty_store
    {
        protected Func<EventStoreDbContext> contextFactory;
        protected EfEventStore sut;
        protected string dbName = Guid.NewGuid().ToString();

        public given_empty_store()
        {
            var serializer = new NewtonsoftJsonSerializer();
            this.contextFactory = () => EventStoreDbContext.ResolveNewInMemoryContext(this.dbName);
            this.sut = new EfEventStore(this.contextFactory, serializer, 
                new EventDeserializationAndVersionManager(serializer, "Infrastructure.EntityFramework.Tests.EventStorage.Helpers", "Infrastructure.EntityFramework.Tests"), new LocalDateTimeProvider());
        }
    }

    public class when_appendig_single_event : given_empty_store
    {
        private string streamName = "tests-1234";
        private string sourceId = "1234";
        private FooEvent evnt;

        public when_appendig_single_event()
        {
            this.evnt = new FooEvent(this.sourceId);

            this.sut.AppendToStreamAsync(this.streamName, new IEvent[] { this.evnt }).Wait();
        }

        [Fact]
        public async Task then_is_persisted()
        {
            using (var context = this.contextFactory())
            {
                Assert.Equal(1, await context.Events.CountAsync());
            }
        }
    }

    public class when_appending_events : given_empty_store
    {
        private List<IEvent> events;
        private string streamName = "tests-1234";
        private string sourceId = "1234";

        public when_appending_events()
        {
            this.events = new List<IEvent>
            {
                new FooEvent(this.sourceId),
                new BarEvent(this.sourceId)
            };

            this.sut.AppendToStreamAsync(this.streamName, this.events).Wait();
        }

        [Fact]
        public async Task then_all_are_persisted()
        {
            using (var context = this.contextFactory())
            {
                Assert.Equal(2, await context.Events.CountAsync());
            }
        }

        [Fact]
        public async Task then_position_is_updated()
        {
            using (var context = this.contextFactory())
            {
                Assert.Equal(0, (await context.Events.FirstAsync()).Position);
                Assert.Equal(1, (await context.Events.LastAsync()).Position);
            }
        }

        [Fact]
        public async Task then_each_event_is_persisted_with_version_in_order()
        {
            using (var context = this.contextFactory())
            {
                Assert.Equal(0, (await context.Events.FirstAsync()).Version);
                Assert.Equal(1, (await context.Events.LastAsync()).Version);
            }
        }

        [Fact]
        public async Task then_stream_category_is_set()
        {
            using (var context = this.contextFactory())
            {
                Assert.True(await context.Events.AllAsync(x => x.Category == "tests"));
            }
        }

        [Fact]
        public async Task then_source_id_is_set()
        {
            using (var context = this.contextFactory())
            {
                Assert.True(await context.Events.AllAsync(x => x.SourceId == this.sourceId));
            }
        }

        [Fact]
        public async Task then_timestamp_is_set()
        {
            using (var context = this.contextFactory())
            {
                Assert.True(await context.Events.AllAsync(x => x.TimeStamp != default(DateTime)));
            }
        }

        [Fact]
        public async Task then_event_type_is_set()
        {
            using (var context = this.contextFactory())
            {
                Assert.Equal(typeof(FooEvent).Name.WithFirstCharInLower(), (await context.Events.FirstAsync()).EventType);
                Assert.Equal(typeof(BarEvent).Name.WithFirstCharInLower(), (await context.Events.LastAsync()).EventType);
            }
        }
    }

    public class when_appending_events_with_expected_version_as_the_stored : given_empty_store
    {
        private List<IEvent> events;
        private string streamName = "tests-1234";
        private string sourceId = "1234";

        public when_appending_events_with_expected_version_as_the_stored()
        {
            this.events = new List<IEvent>
            {
                new FooEvent(this.sourceId),
                new BarEvent(this.sourceId)
            };

            this.sut.AppendToStreamAsync(this.streamName, -1, this.events).Wait();
        }

        [Fact]
        public async Task then_all_are_persisted()
        {
            using (var context = this.contextFactory())
            {
                Assert.Equal(2, await context.Events.CountAsync());
            }
        }
    }

    public class given_store_with_events_of_a_single_category : given_empty_store
    {
        private List<IEvent> storedEvents;
        private string streamName = "tests-1234";
        private string sourceId = "1234";

        public given_store_with_events_of_a_single_category()
        {
            this.storedEvents = new List<IEvent>
            {
                new FooEvent(this.sourceId),
                new BarEvent(this.sourceId),
                new FooEvent(this.sourceId),
                new BarEvent(this.sourceId)
            };

            this.sut.AppendToStreamAsync(this.streamName, -1, this.storedEvents).Wait();
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(2)]
        public async Task when_appending_events_with_wrong_expected_version_then_throws(long wrongExpectedVersion)
        {
            var newEvents = new List<IEvent>
            {
                new FooEvent(this.sourceId),
                new BarEvent(this.sourceId)
            };

            await Assert.ThrowsAsync<OptimisticConcurrencyException>(
                async () => await this.sut.AppendToStreamAsync(this.streamName, wrongExpectedVersion, newEvents));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(2)]
        public async Task when_appending_events_with_wrong_expected_version_then_entire_commit_is_canceled(long wrongExpectedVersion)
        {
            long lastPosition;
            using (var context = this.contextFactory())
            {
                lastPosition = await context.Events.MaxAsync(x => x.Position);
            }

            var newEvents = new List<IEvent>
            {
                new FooEvent(this.sourceId),
                new BarEvent(this.sourceId)
            };

            try
            {
                await this.sut.AppendToStreamAsync(this.streamName, wrongExpectedVersion, newEvents);
            }
            catch { }

            using (var context = this.contextFactory())
            {
                Assert.Equal(lastPosition, await context.Events.MaxAsync(x => x.Position));
            }
        }

        [Theory]
        [InlineData("1234", 3)]
        [InlineData("ABCDE", -1)]
        [InlineData("123-ABC", -1)]
        public async Task when_appending_events_from_different_entities_with_their_expected_versions_then_are_persisted_in_their_respective_streams(string entityId, long expectedVersion)
        {
            long lastPosition;
            using (var context = this.contextFactory())
            {
                lastPosition = await context.Events.MaxAsync(x => x.Position);
            }

            var newEvents = new List<IEvent>
            {
                new FooEvent(entityId),
                new BarEvent(entityId)
            };

            await this.sut.AppendToStreamAsync(this.streamName, expectedVersion, newEvents);

            using (var context = this.contextFactory())
            {
                Assert.Equal(lastPosition + 2, await context.Events.MaxAsync(x => x.Position));
            }
        }

        [Fact]
        public async Task then_can_check_persisted_stream_existence()
        {
            Assert.True(await this.sut.CheckStreamExistenceAsync(this.streamName));
        }

        [Fact]
        public async Task then_can_check_missing_stream_existence()
        {
            Assert.False(await this.sut.CheckStreamExistenceAsync("foo-999"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task then_can_read_single_event_from_the_stream(long start)
        {
            var slice = await this.sut.ReadStreamForwardAsync(this.streamName, start, 1);

            if (start == 3)
                // last event from the stream
                Assert.True(slice.IsEndOfStream);
            else
                Assert.False(slice.IsEndOfStream);

            Assert.Equal(start + 1, slice.NextEventNumber);
            Assert.Equal(SliceFetchStatus.Success, slice.Status);
        }

        [Fact]
        public async Task then_can_not_read_next_event_after_the_last_one()
        {
            var slice = await this.sut.ReadStreamForwardAsync(this.streamName, 3, 1);

            Assert.True(slice.IsEndOfStream);

            slice = await this.sut.ReadStreamForwardAsync(this.streamName, slice.NextEventNumber, 1);

            Assert.Equal(SliceFetchStatus.Success, slice.Status);
            Assert.Empty(slice.Events);
            Assert.True(slice.IsEndOfStream);
        }

        [Theory]
        [InlineData("nonexistentStream-10")]
        [InlineData("nonexistentStream-20")]
        public async Task when_fetching_single_event_from_inexistent_stream_then_does_not_found(string streamName)
        {
            var slice = await this.sut.ReadStreamForwardAsync(streamName, 0, 1);

            Assert.Equal(SliceFetchStatus.StreamNotFound, slice.Status);
            Assert.Null(slice.Events);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task then_can_read_a_slice_of_events_in_order(long start)
        {
            var slice = await this.sut.ReadStreamForwardAsync(this.streamName, start, 2);

            Assert.Equal(SliceFetchStatus.Success, slice.Status);
            Assert.Equal(2, slice.Events.Length);
            Assert.False(slice.IsEndOfStream);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(10)]
        public async Task then_can_read_last_slice_of_events(int count)
        {
            var slice = await this.sut.ReadStreamForwardAsync(this.streamName, 2, count);

            Assert.Equal(SliceFetchStatus.Success, slice.Status);
            Assert.Equal(2, slice.Events.Length);
            Assert.True(slice.IsEndOfStream);
            Assert.Equal(4, slice.NextEventNumber);
        }

        [Fact]
        public async Task when_reading_events_then_gets_in_their_corresponding_type()
        {
            var slice = await this.sut.ReadStreamForwardAsync(this.streamName, 0, 2);

            Assert.True(slice.Events.First() is FooEvent);
            Assert.True(slice.Events.Last() is BarEvent);
        }

        [Fact]
        public async Task when_reading_events_then_gets_the_payload()
        {
            var slice = await this.sut.ReadStreamForwardAsync(this.streamName, 0, int.MaxValue);

            Assert.True(slice.Events.All(x => x.StreamId == this.sourceId));
        }

        [Fact]
        public async Task when_reading_events_then_gets_the_full_metadata()
        {
            var slice = await this.sut.ReadStreamForwardAsync(this.streamName, 0, 2);

            var expected = MetadataHelper.NewEventMetadata();

            var firstMetadata = slice.Events.First().GetEventMetadata();

            Assert.False(default(Guid) == firstMetadata.EventId);
            Assert.Equal(expected.CorrelationId, firstMetadata.CorrelationId);
            Assert.Equal(expected.CausationId, firstMetadata.CausationId);
            Assert.Equal(expected.CommitId, firstMetadata.CommitId);
            Assert.False(default(DateTime) == firstMetadata.Timestamp);
            Assert.Equal("tests", firstMetadata.EventSourcedType);
            Assert.Equal(expected.AuthorId, firstMetadata.AuthorId);
            Assert.Equal(expected.AuthorName, firstMetadata.AuthorName);
            Assert.Equal(expected.ClientIpAddress, firstMetadata.ClientIpAddress);
            Assert.Equal(expected.UserAgent, firstMetadata.UserAgent);
            // In transit only added metadata
            Assert.Equal(0, firstMetadata.EventSourcedVersion);
            Assert.Equal(0, firstMetadata.EventNumber);


            var lastMetadata = slice.Events.Last().GetEventMetadata();

            Assert.False(default(Guid) == lastMetadata.EventId);
            Assert.NotEqual(firstMetadata.EventId, lastMetadata.EventId);
            Assert.Equal(expected.CorrelationId, lastMetadata.CorrelationId);
            Assert.Equal(expected.CausationId, lastMetadata.CausationId);
            Assert.Equal(expected.CommitId, lastMetadata.CommitId);
            Assert.False(default(DateTime) == lastMetadata.Timestamp);
            Assert.Equal("tests", lastMetadata.EventSourcedType);
            Assert.Equal(expected.AuthorId, lastMetadata.AuthorId);
            Assert.Equal(expected.AuthorName, lastMetadata.AuthorName);
            Assert.Equal(expected.ClientIpAddress, lastMetadata.ClientIpAddress);
            Assert.Equal(expected.UserAgent, lastMetadata.UserAgent);
            // In transit only added metadata
            Assert.Equal(1, lastMetadata.EventSourcedVersion);
            Assert.Equal(1, lastMetadata.EventNumber);
        }

        [Fact]
        public async Task when_reading_last_stream_from_category_then_fetchs_stream_name()
        {
            var streamName = await this.sut.ReadLastStreamFromCategory("tests");
            Assert.Equal("1234", streamName);
        }
    }

    public class given_store_with_events_of_multiple_categories : given_empty_store
    {
        private readonly string baseSourceId = "abjk";

        private const string category1 = "tests";
        private const string category2 = "logs";
        private const string category3 = "accounts";

        public given_store_with_events_of_multiple_categories()
        {
            var iterations = 4;
            for (int i = 0; i < iterations; i++)
            {
                var sourceId = this.baseSourceId + i;
                var list = new List<IEvent>
                {
                    new FooEvent(sourceId, category1),
                    new BarEvent(sourceId, category1),
                    new FooEvent(sourceId, category1),
                    new BarEvent(sourceId, category1)
                };

                this.sut.AppendToStreamAsync($"{category1}-{sourceId}", -1, list).Wait();

                list = new List<IEvent>
                {
                    new FooEvent(sourceId, category2),
                    new BarEvent(sourceId, category2),
                    new FooEvent(sourceId, category2),
                    new BarEvent(sourceId, category2)
                };
                this.sut.AppendToStreamAsync($"{category2}-{sourceId}", -1, list).Wait();

                list = new List<IEvent>
                {
                    new FooEvent(sourceId, category3),
                    new BarEvent(sourceId, category3),
                    new FooEvent(sourceId, category3),
                    new BarEvent(sourceId, category3)
                };
                this.sut.AppendToStreamAsync($"{category3}-{sourceId}", -1, list).Wait();
            }
        }

        [Theory]
        [InlineData(category1)]
        [InlineData(category2)]
        [InlineData(category3)]
        public async Task when_getting_streams_from_given_category_then_can_retrieve_only_the_first_one(string category)
        {
            var result = await this.sut.ReadStreamsFromCategoryAsync(category, 0, 1);

            Assert.NotNull(result);
            Assert.Single(result.Streams);
            Assert.Equal(SliceFetchStatus.Success, result.Status);
            Assert.False(result.IsEndOfStream);
            Assert.Equal(1, result.NextEventNumber);
            Assert.Equal($"{category}-{this.baseSourceId + "0"}", result.Streams[0].StreamName);
        }
    }
}
