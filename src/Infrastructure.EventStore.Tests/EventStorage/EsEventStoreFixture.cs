using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using Infrastructure.EventLog;
using Infrastructure.EventStorage;
using Infrastructure.EventStore.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Serialization;
using Infrastructure.Utils;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.EventStore.Tests.EventStorage
{
    public abstract class given_empty_store
    {
        protected EsEventStore sut;
        protected Mock<IEventStoreConnection> connection;
        protected IJsonSerializer serializer;

        public given_empty_store(Mock<IEventStoreConnection> connectionMock = null)
        {
            if (connectionMock is null)
                connectionMock = new Mock<IEventStoreConnection>();

            this.connection = connectionMock;
            this.serializer = new NewtonsoftJsonSerializer();

            this.sut = new EsEventStore(() => Task.FromResult(this.connection.Object), this.serializer, 
                new EventDeserializationAndVersionManager(this.serializer, "Infrastructure.EventStore.Tests.EventStorage", "Infrastructure.EventStore.Tests"),
                new VolatileCheckpointStore(), new Mock<IEventLogReader>().Object);
        }
    }

    public class when_appendig_sigle_event : given_empty_store
    {
        private string streamName = "tests-1234";
        private string sourceId = "1234";
        private FooEvent evnt;

        public when_appendig_sigle_event()
        {
            this.evnt = new FooEvent(this.sourceId);

            this.sut.AppendToStreamAsync(this.streamName, new IEvent[] { this.evnt }).Wait();
        }

        [Fact]
        public void then_is_persisted()
        {
            this.connection.Verify(x => x.AppendToStreamAsync(this.streamName, It.IsAny<long>(), It.Is<EventData[]>(
                events => events.Length == 1)));
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
        public void then_all_are_persisted()
        {
            this.connection.Verify(x => x.AppendToStreamAsync(this.streamName, ExpectedVersion.Any, It.Is<EventData[]>(
                events => events.Length == 2)));
        }

        [Fact]
        public void then_all_are_persisted_in_order()
        {
            this.connection.Verify(c => c.AppendToStreamAsync(this.streamName, ExpectedVersion.Any, It.Is<EventData[]>(
                events => events.First()
                            .Transform(x => Encoding.UTF8.GetString(x.Data))
                            .Transform(x => this.serializer.Deserialize<FooEvent>(x))
                            is FooEvent

                          && events.Last()
                            .Transform(x => Encoding.UTF8.GetString(x.Data))
                            .Transform(x => this.serializer.Deserialize<BarEvent>(x))
                            is BarEvent)));
        }

        //[Fact]
        //public void then_each_event_is_persisted_with_data()
        //{
        //    var expected = MetadataHelper.NewEventMetadata("tests");

        //    this.connection.Verify(c => c.AppendToStreamAsync(this.streamName, ExpectedVersion.Any, It.Is<EventData[]>(
        //        events => events
        //                    .Select(x => Encoding.UTF8.GetString(x.Data))
        //                    .Select(x => this.serializer.Deserialize<IEvent>(x))
        //                    .All(x =>
        //                            x != null
        //                            && x.StreamId == this.sourceId
        //                            && x is FooEvent | x is BarEvent))));
        //}


        [Fact]
        public void then_each_event_is_persisted_with_metadata()
        {
            var expected = MetadataHelper.NewEventMetadata();

            this.connection.Verify(c => c.AppendToStreamAsync(this.streamName, It.IsAny<long>(), It.Is<EventData[]>(
                events => events
                            .Select(x => Encoding.UTF8.GetString(x.Metadata))
                            .Select(x => this.serializer.DeserializeDictionary<string, object>(x))
                            .Select(x => EventMetadata.Parse(x, 0, 0, expected.EventSourcedType))
                            .All(x => 
                                    x != null
                                    && x.AuthorId == expected.AuthorId
                                    && x.AuthorName == expected.AuthorName
                                    && x.CausationId == expected.CausationId
                                    && x.ClientIpAddress == expected.ClientIpAddress
                                    && x.CommitId == expected.CommitId
                                    && x.CorrelationId == expected.CorrelationId
                                    && x.EventId != default(Guid) 
                                    && x.EventId != Guid.Empty
                                    && x.EventSourcedType == expected.EventSourcedType
                                    && x.UserAgent == x.UserAgent))));
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
        public void then_all_are_persisted()
        {
            this.connection.Verify(x => x.AppendToStreamAsync(this.streamName, -1, It.Is<EventData[]>(
                events => events.Length == 2)));
        }
    }

    public class given_store_with_events : given_empty_store
    {
        private List<IEvent> storedEvents;
        private string streamName = "tests-1234";
        private string sourceId = "1234";

        public given_store_with_events()
            : base(new Mock<IEventStoreConnection>()
                  .Tap(c =>
                  {
                      c.Setup(conn => conn.AppendToStreamAsync("tests-1234", It.Is<long>(x => x != 3 && x != ExpectedVersion.Any), It.IsAny<EventData[]>()))
                      .ThrowsAsync(new WrongExpectedVersionException("expected 3"));
                  }))
        {
            this.storedEvents = new List<IEvent>
            {
                new FooEvent(this.sourceId),
                new BarEvent(this.sourceId),
                new FooEvent(this.sourceId),
                new BarEvent(this.sourceId)
            };

            this.sut.AppendToStreamAsync(this.streamName, ExpectedVersion.Any, this.storedEvents).Wait();
        }

        [Theory]
        [InlineData(-1)] // empty
        [InlineData(0)]  // one event
        [InlineData(2)]  // two events
        [InlineData(4)]  // three events
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
    }
}
