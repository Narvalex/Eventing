using Infrastructure.DateTimeProvider;
using Infrastructure.EventSourcing;
using Infrastructure.EventSourcing.Transactions;
using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Processing.WriteLock;
using Infrastructure.Serialization;
using Infrastructure.Snapshotting;
using Infrastructure.Utils;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.EventSourcing
{
    public class given_namespace
    {
        public given_namespace()
        {
            EventSourced.SetValidNamespace("Infrastructure.Tests.EventSourcing");
        }
    }

    public class when_saving_entity : given_namespace
    {
        private string sourceId;
        private string correlationId = Guid.NewGuid().ToString();
        private string causationId = Guid.NewGuid().ToString();
        private Mock<IEventStore> eventStore;
        private readonly ISnapshotRepository snapshotCache;
        private MessageMetadata incomingMetadata = new MessageMetadata("test123", "TestGentleman", "localhost", "Internet Exporer 6, WinVista");

        public when_saving_entity()
        {
            this.eventStore = new Mock<IEventStore>();
            this.snapshotCache = new SnapshotRepository(new NoExclusiveWriteLock(), new NewtonsoftJsonSerializer(), new NoopPersistentSnapshotter(), TimeSpan.FromMinutes(30));

            var sut = new EventSourcedRepository(this.eventStore.Object, this.snapshotCache, new LocalDateTimeProvider(), new OnlineTransactionPool(), new NewtonsoftJsonSerializer(), new Mock<IEventDeserializationAndVersionManager>().Object);
            this.sourceId = "1234";

            var entity = EventSourcedCreator.New<TestEntities>();
            entity.Update(this.correlationId, this.causationId, null, incomingMetadata, true, new TestEvent(this.sourceId, "Bar"));
            entity.Update(this.correlationId, this.causationId, null, incomingMetadata, true, new TestEvent(this.sourceId, "Baz"));

            // Command handling does not have a causation id, since the 
            // commands are not persisted. Only the events
            sut.CommitAsync(entity).Wait();
        }

        [Fact]
        public void then_stores_events_in_event_store()
        {
            this.eventStore.Verify(
                es => es.AppendToStreamAsync(
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.Is<IEnumerable<IEvent>>((
                        x =>
                            x.Count() == 2
                            // First event
                            && x.First().StreamId == "1234"
                            && ((TestEvent)x.First()).Foo == "Bar"
                            // Last event
                            && x.Last().StreamId == "1234"
                            && ((TestEvent)x.Last()).Foo == "Baz")
            )));
        }

        [Fact]
        public void then_stores_with_an_expected_version()
        {
            this.eventStore.Verify(
                es => es.AppendToStreamAsync(
                    It.IsAny<string>(),
                    EventStream.NoEventsNumber,
                    It.IsAny<IEnumerable<IEvent>>()));
        }

        [Fact]
        public void then_uses_composed_stream_name()
        {
            this.eventStore.Verify(
                es => es.AppendToStreamAsync(
                    "testEntities-1234",
                    It.IsAny<long>(),
                    It.IsAny<IEnumerable<IEvent>>()));
        }

        [Fact]
        public void then_stores_event_metadata()
        {
            this.eventStore.Verify(
                es => es.AppendToStreamAsync(
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.Is<IEnumerable<IEvent>>(
                        x =>
                            // First event
                            x.First().GetEventMetadata() != null
                            && x.First().GetEventMetadata().EventId != default(Guid)
                            && x.First().GetEventMetadata().CorrelationId == this.correlationId
                            && x.First().GetEventMetadata().CausationId == this.causationId
                            && x.First().GetEventMetadata().CommitId != default(Guid).ToString()
                            && x.First().GetEventMetadata().Timestamp != default(DateTime)
                            && x.First().GetEventMetadata().EventSourcedVersion == default(long)
                            && x.First().GetEventMetadata().AuthorId == this.incomingMetadata.AuthorId
                            && x.First().GetEventMetadata().AuthorName == this.incomingMetadata.AuthorName
                            && x.First().GetEventMetadata().ClientIpAddress == this.incomingMetadata.ClientIpAddress
                            && x.First().GetEventMetadata().UserAgent == this.incomingMetadata.UserAgent
                            // Last event
                            && x.Last().GetEventMetadata() != null
                            && x.Last().GetEventMetadata().EventId != default(Guid)
                            && x.Last().GetEventMetadata().EventId != x.First().GetEventMetadata().EventId
                            && x.Last().GetEventMetadata().CorrelationId == this.correlationId
                            && x.Last().GetEventMetadata().CausationId == this.causationId
                            && x.Last().GetEventMetadata().CommitId == x.First().GetEventMetadata().CommitId
                            && x.Last().GetEventMetadata().Timestamp != default(DateTime)
                            && x.Last().GetEventMetadata().EventSourcedVersion == default(long)
                            && x.Last().GetEventMetadata().AuthorId == this.incomingMetadata.AuthorId
                            && x.Last().GetEventMetadata().AuthorName == this.incomingMetadata.AuthorName
                            && x.Last().GetEventMetadata().ClientIpAddress == this.incomingMetadata.ClientIpAddress
                            && x.Last().GetEventMetadata().UserAgent == this.incomingMetadata.UserAgent
            )));
        }

        [Fact]
        public void then_stores_in_snapshot_cache()
        {
            Assert.True(this.snapshotCache.TryGetFromMemory<TestEntities>("testEntities-1234", out var snapshot));
            Assert.NotNull(snapshot);
            Assert.Equal(1, snapshot.Metadata.Version);
            Assert.Equal("testEntities-1234", snapshot.Metadata.StreamName);
        }
    }

    public class when_saving_stateless_entity : given_namespace
    {
        private Mock<IEventStore> eventStore = new Mock<IEventStore>();
        private ISnapshotRepository snapshotCache = new SnapshotRepository(new NoExclusiveWriteLock(), new NewtonsoftJsonSerializer(), new NoopPersistentSnapshotter(), TimeSpan.FromMinutes(30));
        private string correlationId = Guid.NewGuid().ToString();
        private string causationId = Guid.NewGuid().ToString();
        private MessageMetadata incomingMetadata = new MessageMetadata("test123", "TestGentleman", "localhost", "Internet Exporer 6, WinVista");

        public when_saving_stateless_entity()
        {
            var sut = new EventSourcedRepository(this.eventStore.Object, this.snapshotCache,
                new LocalDateTimeProvider(), new OnlineTransactionPool(), new NewtonsoftJsonSerializer(), new Mock<IEventDeserializationAndVersionManager>().Object);

            sut.AppendAsync<StatelessTestEntity>(
                new Event[]
                {
                    new LogEvent("1234", "Bar"),
                    new LogEvent("1234", "Baz")
                },
                incomingMetadata,
                this.correlationId,
                this.causationId)
            .Wait();

        }

        [Fact]
        public void then_stores_events_in_event_store_without_expected_version()
        {
            this.eventStore.Verify(
                es => es.AppendToStreamAsync(
                    It.IsAny<string>(),
                    It.Is<IEnumerable<IEvent>>((
                        x =>
                            x.Count() == 2
                            // First event
                            && x.First().StreamId == "1234"
                            && ((LogEvent)x.First()).Foo == "Bar"
                            // Last event
                            && x.Last().StreamId == "1234"
                            && ((LogEvent)x.Last()).Foo == "Baz")
            )));
        }

        [Fact]
        public void then_uses_composed_stream_name()
        {
            this.eventStore.Verify(
                es => es.AppendToStreamAsync(
                    "statelessTestEntity-1234",
                    It.IsAny<IEnumerable<IEvent>>()));
        }

        [Fact]
        public void then_stores_event_metadata()
        {
            this.eventStore.Verify(
                es => es.AppendToStreamAsync(
                    It.IsAny<string>(),
                    It.Is<IEnumerable<IEvent>>(
                        x =>
                            // First event
                            x.First().GetEventMetadata() != null
                            && x.First().GetEventMetadata().EventId != default(Guid)
                            && x.First().GetEventMetadata().CorrelationId == this.correlationId
                            && x.First().GetEventMetadata().CausationId == this.causationId
                            && x.First().GetEventMetadata().CommitId != default(Guid).ToString()
                            && x.First().GetEventMetadata().Timestamp != default(DateTime)
                            && x.First().GetEventMetadata().EventSourcedVersion == default(long)
                            && x.First().GetEventMetadata().AuthorId == this.incomingMetadata.AuthorId
                            && x.First().GetEventMetadata().AuthorName == this.incomingMetadata.AuthorName
                            && x.First().GetEventMetadata().ClientIpAddress == this.incomingMetadata.ClientIpAddress
                            && x.First().GetEventMetadata().UserAgent == this.incomingMetadata.UserAgent
                            // Last event
                            && x.Last().GetEventMetadata() != null
                            && x.Last().GetEventMetadata().EventId != default(Guid)
                            && x.Last().GetEventMetadata().EventId != x.First().GetEventMetadata().EventId
                            && x.Last().GetEventMetadata().CorrelationId == this.correlationId
                            && x.Last().GetEventMetadata().CausationId == x.First().GetEventMetadata().EventId.ToString()
                            && x.Last().GetEventMetadata().CommitId == x.First().GetEventMetadata().CommitId
                            && x.Last().GetEventMetadata().Timestamp != default(DateTime)
                            && x.Last().GetEventMetadata().EventSourcedVersion == default(long)
                            && x.Last().GetEventMetadata().AuthorId == this.incomingMetadata.AuthorId
                            && x.Last().GetEventMetadata().AuthorName == this.incomingMetadata.AuthorName
                            && x.Last().GetEventMetadata().ClientIpAddress == this.incomingMetadata.ClientIpAddress
                            && x.Last().GetEventMetadata().UserAgent == this.incomingMetadata.UserAgent
            )));
        }

        [Fact]
        public void then_does_not_store_in_snapshot_cache()
        {
            Assert.False(this.snapshotCache.TryGetFromMemory<StatelessTestEntity>("testEntities-1234", out var snapshot));
            Assert.Null(snapshot);
        }
    }

    public class given_persisted_events_of_a_single_entity_wihout_any_type_of_caching : given_namespace
    {
        private EventSourcedRepository sut;

        public given_persisted_events_of_a_single_entity_wihout_any_type_of_caching()
        {
            var eventStore = new Mock<IEventStore>();
            var snapshotCache = new Mock<ISnapshotRepository>();

            var metadata1 = new Mock<IEventMetadata>();
            metadata1.Setup(x => x.EventSourcedVersion).Returns(0);
            metadata1.Setup(x => x.CausationId).Returns("1");

            var metadata2 = new Mock<IEventMetadata>();
            metadata2.Setup(x => x.EventSourcedVersion).Returns(1);
            metadata2.Setup(x => x.CausationId).Returns("2");

            var metadata3 = new Mock<IEventMetadata>();
            metadata3.Setup(x => x.EventSourcedVersion).Returns(2);
            metadata3.Setup(x => x.CausationId).Returns("3");

            var events = new IEvent[]
            {
                new TestEventWithMetadata("1234", "Bar", metadata1.Object), // 0
                new TestEventWithMetadata("1234", "Baz", metadata2.Object), // 1
                new TestEventWithMetadata("1234", "FooBar", metadata3.Object) // 2
            };

            eventStore.Setup(x => x.ReadStreamForwardAsync("testEntities-1234", 0, It.IsAny<int>()))
                .ReturnsAsync(new EventStreamSlice(SliceFetchStatus.Success, events, 3, true));

            eventStore.Setup(x => x.CheckStreamExistenceAsync("testEntities-1234"))
                .ReturnsAsync(true);

            eventStore.Setup(x => x.ReadStreamsFromCategoryAsync("testEntities", It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(new CategoryStreamsSlice(SliceFetchStatus.Success, new List<StreamNameAndVersion> { new StreamNameAndVersion("testEntities-1234", 0) }, 1, true));

            eventStore.Setup(x => x.ReadLastStreamFromCategory(It.Is<string>(c => c == "testEntities"), 0))
                .ReturnsAsync("testEntities-1234");

            this.sut = new EventSourcedRepository(eventStore.Object, snapshotCache.Object,
                new LocalDateTimeProvider(), new OnlineTransactionPool(), new NewtonsoftJsonSerializer(), new Mock<IEventDeserializationAndVersionManager>().Object);
        }

        [Fact]
        public async Task when_invoking_generic_api_then_rehydrates()
        {
            var entity = await sut.GetByStreamNameAsync<TestEntities>("testEntities-1234");

            Assert.NotNull(entity);
            Assert.Equal(2, entity.Metadata.Version);
            Assert.Equal("testEntities-1234", entity.Metadata.StreamName);
        }

        [Fact]
        public async Task when_trying_to_rehydrate_to_specific_version_then_rehydrates_only_to_that_version()
        {
            var entity = await sut.GetByStreamNameAsync<TestEntities>("testEntities-1234", 1);

            Assert.NotNull(entity);
            Assert.Equal(1, entity.Metadata.Version);
            Assert.Equal("testEntities-1234", entity.Metadata.StreamName);
        }

        [Fact]
        public async Task when_checking_exixtence_then_confirms_existence()
        {
            Assert.True(await this.sut.ExistsAsync<TestEntities>("testEntities-1234"));
        }

        [Fact]
        public async Task when_getting_last_event_sourced_id_then_gets_last_id()
        {
            var id = await this.sut.GetLastEventSourcedId<TestEntities>();
            Assert.Equal("1234", id);
        }
    }

    public class given_no_entity_al_all : given_namespace
    {
        private EventSourcedRepository sut;

        public given_no_entity_al_all()
        {
            var eventStore = new Mock<IEventStore>();

            eventStore.Setup(x => x.ReadStreamForwardAsync("testEntities-1234", 0, It.IsAny<int>()))
                .ReturnsAsync(new EventStreamSlice(SliceFetchStatus.StreamNotFound, null, 0, true));
            eventStore.Setup(x => x.CheckStreamExistenceAsync("testEntities-1234"))
                .ReturnsAsync(false);
            eventStore.Setup(x => x.ReadStreamsFromCategoryAsync("testEntities", It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(new CategoryStreamsSlice(SliceFetchStatus.StreamNotFound, null, 0, true));

            this.sut = new EventSourcedRepository(eventStore.Object,
                new SnapshotRepository(new NoExclusiveWriteLock(), new NewtonsoftJsonSerializer(), new NoopPersistentSnapshotter(), TimeSpan.FromMinutes(30)),
                new LocalDateTimeProvider(), new OnlineTransactionPool(), new NewtonsoftJsonSerializer(), new Mock<IEventDeserializationAndVersionManager>().Object);
        }

        [Fact]
        public async Task when_getting_then_returns_null()
        {
            var entity = await this.sut.GetByStreamNameAsync<TestEntities>("testEntities-1234");

            Assert.Null(entity);
        }

        [Fact]
        public async Task when_checking_existence_then_returns_false()
        {
            Assert.False(await this.sut.ExistsAsync<TestEntities>("testEntities-1234"));
        }

        [Fact]
        public async Task when_getting_last_id_then_returns_null()
        {
            var id = await this.sut.GetLastEventSourcedId<TestEntities>();
            Assert.Null(id);
        }
    }

    public class when_having_an_in_memory_cached_entity : given_namespace
    {
        private EventSourcedRepository sut;
        private IEventSourced returnValueFromCache;
        private Mock<IEventStore> eventStore;
        private Mock<ISnapshotRepository> cacheMock;

        public when_having_an_in_memory_cached_entity()
        {
            this.eventStore = new Mock<IEventStore>();
            this.cacheMock = new Mock<ISnapshotRepository>();

            var events = new IEventInTransit[]
            {
                new TestEvent("1234", "Bar"),
                new TestEvent("1234", "Baz")
            };
            var fakeId = Guid.NewGuid().ToString();
            var snapshot = EventSourcedCreator.New<TestEntities>();
            events.ForEach(x => snapshot.Update(fakeId, fakeId, null, new MessageMetadata(fakeId, fakeId, fakeId, fakeId), true, x));
            this.returnValueFromCache = snapshot as IEventSourced;
            cacheMock.Setup(x => x.TryGetFromMemory("testEntities-1234", out returnValueFromCache)).Returns(true);

            this.sut = new EventSourcedRepository(
                eventStore.Object,
                cacheMock.Object,
                new LocalDateTimeProvider(),
                new OnlineTransactionPool(),
                new NewtonsoftJsonSerializer(), new Mock<IEventDeserializationAndVersionManager>().Object);
        }

        [Fact]
        public async Task then_rehydrates_only_from_cached_snapshot()
        {
            var entity = await sut.GetByStreamNameAsync<TestEntities>("testEntities-1234");

            Assert.NotNull(entity);
            Assert.Equal(1, entity.Metadata.Version);
            Assert.Equal("testEntities-1234", entity.Metadata.StreamName);

            eventStore.Verify(x => x.ReadStreamForwardAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>()), Times.Never);
            cacheMock.Verify(x => x.TryGetFromMemory("testEntities-1234", out returnValueFromCache));
        }
    }

    public class given_persistent_snapshot_but_missing_in_memory : given_namespace
    {
        private Mock<ISnapshotRepository> cacheMock;
        private Mock<IEventStore> eventStore;
        private EventSourcedRepository sut;
        private TestEntities snapshot;
        private IEventSourced snapshotInterface;

        public given_persistent_snapshot_but_missing_in_memory()
        {
            this.eventStore = new Mock<IEventStore>();
            this.cacheMock = new Mock<ISnapshotRepository>();

            // arrange persisted snapshot mock
            this.snapshot = EventSourcedCreator.New<TestEntities>();
            var fakeId = "asfsd";
            snapshot.Update(fakeId, fakeId, null, new MessageMetadata(fakeId, fakeId, fakeId, fakeId), true, new TestEvent("1234", "Bar"));

            this.snapshotInterface = this.snapshot;

            // arrange call to event store
            var metadata = new Mock<IEventMetadata>();
            metadata.Setup(x => x.EventSourcedVersion).Returns(1);
            metadata.Setup(x => x.CausationId).Returns("1");

            eventStore
                .Setup(x => x.ReadStreamForwardAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(new EventStreamSlice(SliceFetchStatus.Success, new IEvent[] { new TestEventWithMetadata("1234", "Bar", metadata.Object) }, 2, true));

            this.sut = new EventSourcedRepository(
                eventStore.Object,
                cacheMock.Object,
                new LocalDateTimeProvider(),
                new OnlineTransactionPool(),
                new NewtonsoftJsonSerializer(),
                new Mock<IEventDeserializationAndVersionManager>().Object);
        }
    }
}
