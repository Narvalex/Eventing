using Infrastructure.DateTimeProvider;
using Infrastructure.EntityFramework.EventStorage;
using Infrastructure.EntityFramework.EventStorage.Database;
using Infrastructure.EventSourcing;
using Infrastructure.EventSourcing.Transactions;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Processing.WriteLock;
using Infrastructure.Serialization;
using Infrastructure.Snapshotting;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Erp.Domain.Tests.Helpers
{
    public class TestableEventHandler
    {
        private readonly EsRepositoryTestDecorator repository;
        private readonly IEventHandler eventHandler;
        private readonly ModelSerializationTestHelper serializatonTestHelper;

        // Test Inputs
        private List<Tuple<Type, IEventInTransit[]>> givenHistory = new List<Tuple<Type, IEventInTransit[]>>();
        private List<IEventInTransit> whenList = new List<IEventInTransit>();

        public TestableEventHandler(Func<IEventSourcedRepository, IEventHandler> handlerFactory, string validNamespace, string assembly)
        {
            EventSourced.SetValidNamespace(validNamespace);

            var dbName = Guid.NewGuid().ToString();
            var serializer = new NewtonsoftJsonSerializer();
            var dateTime = new LocalDateTimeProvider();
            var eventVersionManager = new EventDeserializationAndVersionManager(serializer, validNamespace, assembly);
            var eventStore = new EfEventStore(() => EventStoreDbContext.ResolveNewInMemoryContext(dbName), serializer,
                eventVersionManager, dateTime);
            var cache = new SnapshotRepository(new NoExclusiveWriteLock(), serializer, new NoopPersistentSnapshotter(), TimeSpan.FromMinutes(30));
            var pool = new OnlineTransactionPool();
            this.repository = new EsRepositoryTestDecorator(serializer, new EventSourcedRepository(eventStore, cache, dateTime, pool, serializer, eventVersionManager), cache, pool, eventVersionManager);
            this.eventHandler = handlerFactory(this.repository);
            this.serializatonTestHelper = new ModelSerializationTestHelper(serializer);
        }

        public TestableEventHandler Given<TEventSourced>(params IEventInTransit[] history) where TEventSourced : class, IEventSourced
        {
            this.givenHistory.Add(new Tuple<Type, IEventInTransit[]>(typeof(TEventSourced), history));
            return this;
        }

        public TestableEventHandler When(IEventInTransit message)
        {
            this.whenList.Add(message);
            return this;
        }

        public async Task Then<TEvent>(Action<TEvent> predicate = null)
        {
            await this.ExecuteEventHandlerIfNeeded();

            var events = this.repository.LastEvents;

            Assert.True(events.Any(x => x.GetType() == typeof(TEvent)), $"The event type {typeof(TEvent).Name} was not emitted");

            if (predicate == null)
                return;

            predicate.Invoke(events.OfType<TEvent>().Single());
        }

        public async Task Then<TEvent>(string expectedStreamId, Action<TEvent> predicate = null) where TEvent : IEvent
        {
            await this.Then<TEvent>(predicate);
            var e = this.repository.LastEvents.OfType<TEvent>().First();
            Assert.Equal(expectedStreamId, e.StreamId);
        }

        public async Task Then<TEvent>(string expectedStreamId, Func<TEvent, string> expectedEventPropertyResolver, Action<TEvent> predicate = null)
        {
            await this.Then<TEvent>(predicate);
            var e = this.repository.LastEvents.OfType<TEvent>().First();
            AssertX.Equal(expectedStreamId, expectedEventPropertyResolver(e), (IEvent)e);
        }

        public async Task Then<TEvent>(string expectedStreamId, Func<TEvent, string> expectedEventPropertyResolver, string expectedCorrelationId, Action<TEvent> predicate = null)
        {
            await this.Then<TEvent>(expectedStreamId, expectedEventPropertyResolver, predicate);
            var e = this.repository.LastEvents.OfType<TEvent>().First();
            Assert.Equal(expectedCorrelationId, ((IEvent)e).GetCorrelationId());
        }

        public async Task Then<TEvent>(string expectedStreamId, string expectedCorrelationId, Action<TEvent> predicate = null) where TEvent : IEvent
        {
            await this.Then<TEvent>(expectedStreamId, predicate);
            var e = this.repository.LastEvents.OfType<TEvent>().First();
            Assert.Equal(expectedCorrelationId, e.GetCorrelationId());
        }

        public async Task ThenSome<TEvent>(Action<IList<TEvent>> predicate = null)
        {
            await this.ExecuteEventHandlerIfNeeded();
            var events = this.repository.LastEvents;
            Assert.True(events.Any(x => x.GetType() == typeof(TEvent)), $"The event type {typeof(TEvent).Name} was not emitted");
            if (predicate == null)
                return;

            predicate.Invoke(events.OfType<TEvent>().ToList());
        }

        public async Task ThenOnly<TEvent>(Action<TEvent> predicate = null)
        {
            await this.Then(predicate);
            var events = this.repository.LastEvents;
            Assert.Single(events);
        }

        public async Task ThenOnly<TEvent>(string expectedStreamId, Action<TEvent> predicate = null) where TEvent : IEvent
        {
            await this.ThenOnly<TEvent>(predicate);
            var e = this.repository.LastEvents.OfType<TEvent>().First();
            Assert.Equal(expectedStreamId, e.StreamId);
        }

        public async Task ThenOnly<TEvent>(string expectedStreamId, Func<TEvent, string> expectedEventPropertyResolver, Action<TEvent> predicate = null)
        {
            await this.ThenOnly<TEvent>(predicate);
            var e = this.repository.LastEvents.OfType<TEvent>().First();
            AssertX.Equal(expectedStreamId, expectedEventPropertyResolver(e), (IEvent)e);
        }

        public async Task ThenOnly<TEvent>(string expectedStreamId, Func<TEvent, string> expectedEventPropertyResolver, string expectedCorrelationId, Action<TEvent> predicate = null)
        {
            await this.ThenOnly<TEvent>(expectedStreamId, expectedEventPropertyResolver, predicate);
            var e = this.repository.LastEvents.OfType<TEvent>().First();
            Assert.Equal(expectedCorrelationId, ((IEvent)e).GetCorrelationId());
        }

        public async Task ThenOnly<TEvent>(string expectedStreamId, string expectedCorrelationId, Action<TEvent> predicate = null) where TEvent : IEvent
        {
            await this.ThenOnly<TEvent>(expectedStreamId, predicate);
            var e = this.repository.LastEvents.OfType<TEvent>().First();
            Assert.Equal(expectedCorrelationId, e.GetCorrelationId());
        }

        // The signature Action<collection, int> with an int is helpfull for Visual studio to load the 
        // correct overload.
        public async Task Then<TEvent>(Action<IEnumerable<TEvent>, int> predicate = null)
        {
            await this.ExecuteEventHandlerIfNeeded();
            var events = this.repository.LastEvents;
            Assert.Contains(events, x => x.GetType() == typeof(TEvent));

            if (predicate == null)
                return;

            var list = events.OfType<TEvent>();
            predicate.Invoke(list, list.Count());
        }

        public async Task ThenNoOp()
        {
            await this.ExecuteEventHandlerIfNeeded();
            Assert.True(this.repository.LastEvents is null || this.repository.LastEvents.Count() < 1, "There are emitted events!");
        }

        private static IEventMetadata NewMetadata(IEvent @event)
        {
            var eventMetadata = @event.GetEventMetadata();
            var corrId = eventMetadata?.CorrelationId;
            if (corrId.IsEmpty())
                corrId = "corrId";
            var causId = eventMetadata?.CausationId;
            if (causId.IsEmpty())
                causId = Guid.NewGuid().ToString();
            var eventId = eventMetadata?.EventId;
            if (!eventId.HasValue || (eventId.HasValue && eventId.ToString() == default(Guid).ToString()))
                eventId = Guid.NewGuid();

            var newMetadata = new EventMetadata(eventId.Value, corrId, causId, "commitId", DateTime.Now,
                "TestAuthorId1234", "TestAuthor", "localhost", "Windows Vista");

            newMetadata.SetOfEventNumberForTestOnly(eventMetadata?.EventNumber ?? 0);
            return newMetadata;
        }

        private async Task<IEventSourced> ResolveEventSourced(Type eventSourcedtype, string streamId)
        {
            var eventSourced = await this.repository.TryGetByIdAsync(eventSourcedtype, streamId);
            return eventSourced == null ? (IEventSourced)EventSourcedCreator.New(eventSourcedtype) : eventSourced;
        }

        private async Task ExecuteEventHandlerIfNeeded()
        {
            if (this.givenHistory.Any())
            {
                foreach (var tuple in this.givenHistory)
                {
                    var history = tuple.Item2;
                    if (history.Length == 0) continue;

                    var cmd = new TestCommandMock();

                    var es = await this.ResolveEventSourced(tuple.Item1, history[0].StreamId);

                    await history
                    .ForEachAsync(async e =>
                    {
                        var metadata = e.GetEventMetadata();
                        var corrId = metadata?.CorrelationId;
                        if (corrId.IsEmpty())
                            corrId = Guid.NewGuid().ToString();
                        var causId = metadata?.CausationId;
                        if (causId.IsEmpty())
                            causId = Guid.NewGuid().ToString();

                        es.Update(corrId, causId, metadata?.CausationNumber, new MessageMetadata("admin", "admin", "localhost", "visual studio"), true, e);
                        await this.repository.CommitAsyncForGiven(es);
                    });

                    this.repository.ClearLastEvents();
                }

                this.givenHistory.Clear();
            }

            if (this.whenList.Any())
            {
                foreach (var message in this.whenList)
                {
                    this.serializatonTestHelper.EnsureSerializationIsValid(message);

                    this.repository.ClearLastEvents(); // we cleared again (after given) if the when is called multiple times in a single test.
                    var metadata = NewMetadata(message);
                    message.SetEventMetadata(metadata, message.GetType().Name.WithFirstCharInLower());
                    await ((dynamic)eventHandler).Handle((dynamic)message);
                }

                this.whenList.Clear();
            }
        }
    }
}
