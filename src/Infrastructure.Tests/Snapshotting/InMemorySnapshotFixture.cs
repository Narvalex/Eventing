using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Processing.WriteLock;
using Infrastructure.Serialization;
using Infrastructure.Snapshotting;
using Infrastructure.Tests.EventSourcing;
using System;
using Xunit;

namespace Infrastructure.Tests.Snapshotting
{
    public class given_snapshot_cache_class
    {
        [Fact]
        public void when_creating_instance_with_zero_time_span_to_live_then_throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SnapshotRepository(new NoExclusiveWriteLock(), new NewtonsoftJsonSerializer(), new NoopPersistentSnapshotter(), TimeSpan.Zero));
        }

        [Fact]
        public void when_creating_instance_with_min_value_span_to_live_then_throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SnapshotRepository(new NoExclusiveWriteLock(), new NewtonsoftJsonSerializer(), new NoopPersistentSnapshotter(), TimeSpan.MinValue));
        }

    }

    public class given_in_memory_snapshot_cache
    {
        private readonly SnapshotRepository sut;

        public given_in_memory_snapshot_cache()
        {
            EventSourced.SetValidNamespace("Infrastructure.Tests.EventSourcing");
            this.sut = new SnapshotRepository(new NoExclusiveWriteLock(), new NewtonsoftJsonSerializer(), new NoopPersistentSnapshotter(), TimeSpan.FromMinutes(10));
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("bar")]
        public void when_trying_to_get_non_cached_state_then_returns_null(string streamName)
        {
            Assert.False(this.sut.TryGetFromMemory<SnapshotTestEntity>(streamName, out var state));
            Assert.Null(state);
        }

        [Theory]
        [InlineData("Foo", 30)]
        [InlineData("Bar", 18)]
        public void when_caching_state_then_can_retrieve_it(string name, int age)
        {
            var id = Guid.NewGuid().ToString();

            var state = EventSourcedCreator.New<SnapshotTestEntity>();

            var fakeId = "fadsfas";
            state.Update(fakeId, fakeId, null, new MessageMetadata(fakeId, fakeId, fakeId, fakeId), true, new DataSet(id, name, age));

            this.sut.Save(state);
            Assert.True(this.sut.TryGetFromMemory<SnapshotTestEntity>(EventStream.GetStreamName<SnapshotTestEntity>(id), out var cached));
            Assert.NotNull(cached);
            Assert.Equal(state.Metadata.StreamName, cached.Metadata.StreamName);
            Assert.Equal(state.Metadata.Version, cached.Metadata.Version);
            Assert.True(cached is SnapshotTestEntity);
            Assert.Equal(state.Name, ((SnapshotTestEntity)cached).Name);
            Assert.Equal(state.Age, ((SnapshotTestEntity)cached).Age);
        }
    }
}
