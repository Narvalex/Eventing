using Infrastructure.DateTimeProvider;
using Infrastructure.EventSourcing.Transactions;
using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using Infrastructure.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.EventSourcing
{
    public abstract class EventSourced : IEventSourced, IHandlerRegistry
    {
        private static readonly ConcurrentDictionary<Type, string> assemblyNamesByType = new ConcurrentDictionary<Type, string>();
        private static string validNamespace = null!;

        private readonly string entityTypeName;
        private readonly string assemblyName;

        private readonly StreamNameObject streamNameObject;
        private string commitId = null;
        private readonly List<IEvent> newEvents = new List<IEvent>();

        private bool handlersWhereResolved = false;
        private readonly IDictionary<Type, Action<object>> handlers = new Dictionary<Type, Action<object>>();
        private readonly List<Action> outputStateSectionActions = new List<Action>();
        private readonly HashSet<Type> ignoredEvents = new HashSet<Type>();
        private readonly bool isSaga;
        private readonly bool isMutexSaga;
        private bool duplicateIncomingMessageDetected = false;
        private long version = EventStream.NoEventsNumber;
        private long lastCausationNumber = EventStream.NoEventsNumber;
        private bool isLocked = false;
        private string? lockOwnerId = null;
        private bool exists = false;

        private bool prepareEventsAreSet = false;
        private UpdateEventSourcedParams? prepareEventParams;

        public EventSourced(EventSourcedMetadata metadata)
        {
            if (validNamespace.IsEmpty())
                throw new InvalidOperationException("The valid namespace for event sourced entities has not been set.");

            this.streamNameObject = new StreamNameObject(this, metadata.StreamName);
            this.version = metadata.Version;
            var type = Ensured.NamespaceIsValid(this.GetType(), validNamespace);

            this.entityTypeName = type.FullName!;
            this.assemblyName = assemblyNamesByType.GetOrAdd(type, GetAssembly);

            this.lastCausationNumber = metadata.LastCausationNumber;
            this.isLocked = metadata.IsLocked;
            this.lockOwnerId = metadata.LockOwnerId;
            this.exists = metadata.Exists;

            this.isSaga = this is ISagaExecutionCoordinator;
            this.isMutexSaga = this is IMutexSagaExecutionCoordinator;
        }

        public string Id => this.streamNameObject.StreamId;
        public EventSourcedMetadata Metadata => new EventSourcedMetadata(
            this.streamNameObject.Name,
            this.version,
            this.lastCausationNumber,
            this.exists,
            this.isLocked,
            this.lockOwnerId);

        protected abstract void OnRegisteringHandlers(IHandlerRegistry registry);

        void IEventSourced.ApplyOutputState()
        {
            this.OnOutputState();
            this.outputStateSectionActions.ForEach(x => x());
        }

        protected virtual void OnOutputState() { }

        public static void SetValidNamespace(string validNamespace)
        {
            if (EventSourced.validNamespace.IsEmpty())
                EventSourced.validNamespace = Ensured.NotEmpty(validNamespace, nameof(validNamespace));
        }

        IHandlerRegistry IHandlerRegistry.On(Type eventType)
        {
            var type = Ensured.NamespaceIsValid(eventType, validNamespace);
            if (this.ignoredEvents.Contains(type))
                throw new InvalidOperationException("Can not add duplicate ignored event in event sourced");

            this.ignoredEvents.Add(type);
            return this;
        }

        IHandlerRegistry IHandlerRegistry.On(Type eventType, Action<object> handler)
        {
            var type = Ensured.NamespaceIsValid(eventType, validNamespace);
            if (this.handlers.ContainsKey(type))
                throw new InvalidOperationException("Can not add duplicate handlers in event sourced.");

            this.handlers[type] = e => handler(e);
            return this;
        }

        IHandlerRegistry IHandlerRegistry.On<T>(Action<T> handler)
        {
            var type = Ensured.NamespaceIsValid(typeof(T), validNamespace);
            if (this.handlers.ContainsKey(type))
                throw new InvalidOperationException("Can not add duplicate handlers in event sourced.");

            this.handlers[type] = e => handler((T)e);
            return this;
        }

        IHandlerRegistry IHandlerRegistry.On<T>()
        {
            var type = Ensured.NamespaceIsValid(typeof(T), validNamespace);
            if (this.ignoredEvents.Contains(type))
                throw new InvalidOperationException("Can not add duplicate ignored event in event sourced");

            this.ignoredEvents.Add(type);
            return this;
        }

        IHandlerRegistry IHandlerRegistry.AddSection(EventSourcedSection section)
        {
            ((IEventSourcedSection)section).SetRoot(this);
            return this;
        }

        IHandlerRegistry IHandlerRegistry.AddSubEntities(ISubEntities entities)
        {
            entities.OnRegisteringHandlers(this);
            return this;
        }

        protected virtual void OnPrepareEvent(IEvent @event) { }

        protected void Delete() => this.exists = false;

        void IEventSourced.Apply(IEvent @event)
        {
            this.streamNameObject.SetNameIfNeeded(@event);
            this.RegisterHandlersIfNeeded();

            var eventType = @event.GetType();
            if (this.handlers.TryGetValue(eventType, out Action<object> handler))
            {
                this.MarkExistenceIfApplicable(@event);
                handler.Invoke(@event);
            }
            else if (!this.ignoredEvents.Contains(eventType))
                throw new NotImplementedException($"The event sourced entity of type {this.GetType().Name} is missing event handler for {eventType.Name} event.");
            else
                this.MarkExistenceIfApplicable(@event);


            var metadata = @event.GetEventMetadata();
            if (CausationMessageIsAnEvent(metadata.CausationNumber) && this.newEvents.Count == 0)
                this.lastCausationNumber = metadata.CausationNumber!.Value;

            this.version++;
        }

        private void MarkExistenceIfApplicable(IEvent @event)
        {
            // Exists check
            if (!this.exists)
            {
                if (@event is not EntityEvent)
                    this.exists = true;
            }
        }

        private void RegisterHandlersIfNeeded()
        {
            if (!this.handlersWhereResolved)
            {
                var register = (IHandlerRegistry)this;
                this.OnRegisteringHandlers(register);

                register
                    .On<LockAcquired>(e =>
                    {
                        this.isLocked = true;
                        this.lockOwnerId = e.LockOwnerId;
                    })
                    .On<LockReleased>(e =>
                    {
                        this.isLocked = false;
                        this.lockOwnerId = null;
                    })
                ;

                this.handlersWhereResolved = true;
            }
        }

        public virtual void Update(string correlationId, string causationId, long? causationNumber, IMessageMetadata metadata, bool isCommandMetadata, IEventInTransit e)
        {
            if (this.isLocked) // is locked is never true in MulEntityTransactionCoordinator event sourced entity.
            {
                if (e is not EntityEvent)
                {
                    if (!e.CheckIfEventBelongsToTransaction(this.lockOwnerId!))
                        throw new OptimisticConcurrencyException("The event sourced entity is locked by transaction id: " + this.lockOwnerId);
                }
            }

            if (this.duplicateIncomingMessageDetected) return;

            var verifyIdempotency = CausationMessageIsAnEvent(causationNumber) && this.newEvents.Count == 0;
            if (verifyIdempotency && !(causationNumber!.Value > this.lastCausationNumber))
            {
                this.duplicateIncomingMessageDetected = true;
                return;
            }

            if (this.commitId is null)
                this.commitId = Guid.NewGuid().ToString();

            // Saga Check
            if (this.isSaga && !this.isMutexSaga && (this.version > EventStream.NoEventsNumber))
                correlationId = this.streamNameObject.StreamId;

            if (e is PersistentCommand && !this.isSaga)
                throw new InvalidOperationException($"Can not prepare a persistent command because the entity {this.GetType().Name} is not an event sourced saga");

            if (e is MutexPersistentCommand && !this.isMutexSaga)
                throw new InvalidOperationException($"Can not prepare a persistent command because the entity {this.GetType().Name} is not an event sourced mutex saga");

            // After check passes
            if (!this.prepareEventsAreSet)
            {
                this.prepareEventParams = new UpdateEventSourcedParams(correlationId, causationId, causationNumber, metadata, isCommandMetadata);
                this.prepareEventsAreSet = true;
            }

            e.SetEventMetadata(new EventMetadata(
                Guid.NewGuid(),
                correlationId,
                causationId,
                this.commitId,
                DefaultDateTimeProvider.Get().Now,
                metadata.AuthorId,
                metadata.AuthorName,
                metadata.ClientIpAddress,
                metadata.UserAgent,
                causationNumber,
                metadata.DisplayMode,
                isCommandMetadata ? metadata.CommandTimestamp : default,
                metadata.PositionLatitude,
                metadata.PositionLongitude,
                metadata.PositionAccuracy,
                metadata.PositionAltitude,
                metadata.PositionAltitudeAccuracy,
                metadata.PositionHeading,
                metadata.PositionSpeed,
                metadata.PositionTimestamp,
                metadata.PositionError
            ), null);

            this.OnPrepareEvent(e);

            ((IEventSourced)this).Apply(e);
            this.OnOutputState();
            this.newEvents.Add(e);
        }

        IEnumerable<IEventInTransit> IEventSourced.ExtractPendingEvents()
        {
            var events = new IEventInTransit[this.newEvents.Count];
            this.newEvents.CopyTo(events);
            this.newEvents.Clear();
            this.commitId = null;
            this.duplicateIncomingMessageDetected = false;
            this.prepareEventParams = null;
            this.prepareEventsAreSet = false;
            return events;
        }

        protected string StreamId => this.streamNameObject.StreamId;

        string IEventSourced.GetEntityType() => this.entityTypeName;

        string IEventSourced.GetAssembly() => this.assemblyName;

        IEnumerable<Type> IEventSourced.GetSourcingEventTypes() => this.handlers.Keys;

        UpdateEventSourcedParams? IEventSourced.GetPrepareEventParams() => this.prepareEventParams;

        int IEventSourced.GetPendingEventsCount() => this.newEvents.Count;

        private static string GetAssembly(Type type) => type.Assembly.GetName().Name;

        private static bool CausationMessageIsAnEvent(long? causationNumber) => causationNumber.HasValue;

        void IEventSourced.RegisterOutputSectionStateAction(Action action) => this.outputStateSectionActions.Add(action);
    }
}
