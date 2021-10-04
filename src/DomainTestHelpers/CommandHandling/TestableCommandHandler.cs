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
using Xunit.Sdk;

namespace Erp.Domain.Tests.Helpers
{
    public class TestableCommandHandler
    {
        private readonly EsRepositoryTestDecorator repository;
        private readonly ICommandBus commandBus;
        private IHandlingResult? lastHandlingResult;
        private readonly ModelSerializationTestHelper serializationTest;
        private Func<ICommandInTest, ICommandInTest> cmdSerializationTestTransformation = c => c;

        // Test Inputs
        private List<Tuple<Type, IEventInTransit[]>> givenHistory = new List<Tuple<Type, IEventInTransit[]>>();
        private List<Tuple<ICommandInTest, MessageMetadata?>> whenList = new List<Tuple<ICommandInTest, MessageMetadata?>>();

        public TestableCommandHandler(Func<IEventSourcedRepository, ICommandHandler> handlerFactory, string validNamespace, string assembly)
        {
            EventSourced.SetValidNamespace(validNamespace);
            var dbName = Guid.NewGuid().ToString();
            var serializer = new NewtonsoftJsonSerializer();
            var dateTime = new LocalDateTimeProvider();
            var versionManager = new EventDeserializationAndVersionManager(serializer, validNamespace, assembly);
            var eventStore = new EfEventStore(() => EventStoreDbContext.ResolveNewInMemoryContext(dbName), serializer, 
                versionManager, dateTime);
            var cache = new SnapshotRepository(new NoExclusiveWriteLock(), serializer, new NoopPersistentSnapshotter(), TimeSpan.FromMinutes(30));
            var pool = new OnlineTransactionPool();
            this.repository = new EsRepositoryTestDecorator(serializer, new EventSourcedRepository(eventStore, cache, dateTime, pool, serializer, versionManager), cache, pool, versionManager);
            this.commandBus = this.ResolveCommandBus(handlerFactory.Invoke(this.repository), validNamespace);
            this.serializationTest = new ModelSerializationTestHelper(serializer);
        }

        public void SetOnEnsureCommandSerializationIsValid(Func<ICommandInTest, ICommandInTest> transformation)
        {
            this.cmdSerializationTestTransformation = transformation;
        }

        public TestableCommandHandler Given<TEventSourced>(params IEventInTransit[] history) where TEventSourced : class, IEventSourced
        {
            this.givenHistory.Add(new Tuple<Type, IEventInTransit[]>(typeof(TEventSourced), history));
            return this;
        }


        public TestableCommandHandler When(ICommandInTest command, MessageMetadata? metadata = null)
        {
            this.whenList.Add(new Tuple<ICommandInTest, MessageMetadata?>(command, metadata));
            return this;
        }

        public async Task Then<TEvent>(Action<TEvent> predicate = null)
        {
            await this.ExecuteCommandIfNeeded();
            var events = this.repository.LastEvents;
            var count = events.Where(x => x.GetType() == typeof(TEvent)).Count();
            if (count == 0)
                throw new InvalidOperationException(
                    this.AddRejectMessagesIfApplicable($"The event of type {typeof(TEvent).Name} was not emmited"));

            if (count > 1)
                throw new InvalidOperationException(
                    this.AddRejectMessagesIfApplicable($"The event of type {typeof(TEvent).Name} was emmited {count} times. Only once was expected."));

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

        public async Task ThenAll<TEvent>(Action<IList<TEvent>> predicate = null)
        {
            await this.ExecuteCommandIfNeeded();

            var events = this.repository.LastEvents;
            Assert.True(events.Any(x => x.GetType() == typeof(TEvent)),
                this.AddRejectMessagesIfApplicable($"The event type {typeof(TEvent).Name} was not emitted"));
            if (predicate == null)
                return;

            predicate.Invoke(events.OfType<TEvent>().ToList());
        }

        public async Task ThenOnly<TEvent>(Action<TEvent> predicate = null)
        {
            await this.Then(predicate);

            var events = this.repository.LastEvents;
            if (events.Count != 1)
                throw new InvalidOperationException(
                    this.AddRejectMessagesIfApplicable($"A single event of type {typeof(TEvent).Name} was expected"));
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

        // The signature Action<collection, int> with an int is helpfull for Visual studio to load the 
        // correct overload.
        public async Task Then<TEvent>(Action<IEnumerable<TEvent>, int> predicate = null)
        {
            await this.ExecuteCommandIfNeeded();
            var events = this.repository.LastEvents;
            Assert.Contains(events, x => x.GetType() == typeof(TEvent));

            if (predicate == null)
                return;

            var list = events.OfType<TEvent>();
            predicate.Invoke(list, list.Count());
        }

        public async Task ThenNoOp()
        {
            await this.ExecuteCommandIfNeeded();
            Assert.True(this.repository.LastEvents is null || this.repository.LastEvents.Count() < 1, "There are emitted events!");
        }

        public async Task ThenCommandIsAccepted()
        {
            await this.ExecuteCommandIfNeeded();
            if (!this.lastHandlingResult.Success)
                throw new TrueException(this.AddRejectMessagesIfApplicable("The command was not accepted"), false);
        }

        public async Task ThenCommandIsAccepted<T>(Action<T> predicate = null)
        {
            await this.ExecuteCommandIfNeeded();
            if (predicate != null)
            {
                var result = this.lastHandlingResult as Response<T>;
                predicate.Invoke(result.Payload);
            }
        }

        public async Task ThenCommandIsRejected()
        {
            await this.ExecuteCommandIfNeeded();
            Assert.False(this.lastHandlingResult.Success, $"The command was not rejected.");
        }

        public Task ThenThrowsForeignKeyViolationException()
        {
            return Assert.ThrowsAsync<ForeignKeyViolationException>(() => this.ExecuteCommandIfNeeded());
        }

        private string AddRejectMessagesIfApplicable(string message)
        {
            if (this.lastHandlingResult is null || this.lastHandlingResult.Success)
                return message;

            return this.lastHandlingResult
                .Messages
                .Aggregate(message, (a, m) => $"{a}. {m}");
        }

        private ICommandBus ResolveCommandBus(ICommandHandler handler, string validNamespace)
        {
            var commandBus = new CommandBus(validNamespace, new NoExclusiveWriteLock());
            //var commandBus = new CommandBusDynamic();
            commandBus.Register(handler);
            return commandBus;
        }

        private static MessageMetadata NewMetadata()
        {
            return new MessageMetadata("admin", "admin", "localhost", "Windows Vista");
        }

        private async Task<IEventSourced> ResolveEventSourced(Type eventSourcedtype, string streamId)
        {
            var eventSourced = await this.repository.TryGetByIdAsync(eventSourcedtype, streamId);
            return eventSourced == null ? (IEventSourced)EventSourcedCreator.New(eventSourcedtype) : eventSourced;
        }

        private async Task ExecuteCommandIfNeeded()
        {
            // Given
            if (this.givenHistory.Any())
            {
                foreach (var tuple in this.givenHistory)
                {
                    var history = tuple.Item2;
                    var cmd = new TestCommandMock();

                    var es = await this.ResolveEventSourced(tuple.Item1, history[0].StreamId);

                    await history
                    .ForEachAsync(async e =>
                    {
                        var corrId = e.GetEventMetadata()?.CorrelationId;
                        if (corrId.IsEmpty())
                            es.Update(cmd, e);
                        else
                            es.Update(new TestCommandMock(corrId), e);

                        await this.repository.CommitAsyncForGiven(es);
                    });

                    this.repository.ClearLastEvents();
                }

                this.givenHistory.Clear();
            }

            // When
            if (this.whenList.Any())
            {
                foreach (var tuple in this.whenList)
                {
                    var cmd = tuple.Item1;
                    var metadata = tuple.Item2;

                    if (metadata is null)
                        metadata = NewMetadata();

                    this.repository.ClearLastEvents(); // we cleared again (after given) if the when is called multiple times in a single test.

                    this.serializationTest.EnsureSerializationIsValid(this.cmdSerializationTestTransformation(cmd));

                    this.lastHandlingResult = await this.commandBus.Send(cmd, metadata);
                }

                this.whenList.Clear();
            }

        }
    }
}
