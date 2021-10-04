using Infrastructure.EventSourcing.Transactions;
using Infrastructure.IdGeneration;
using Infrastructure.Logging;
using Infrastructure.Messaging.Versioning;
using Infrastructure.Serialization;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;

namespace Infrastructure.Messaging
{
    public class EventDeserializationAndVersionManager : IEventUpcasterRegistry, IEventDeserializationAndVersionManager
    {
        private readonly IJsonSerializer serializer;
        private ILogLite logger = LogManager.GetLoggerFor<EventDeserializationAndVersionManager>();
        private Dictionary<string, IEventUpcaster> upcasters = new Dictionary<string, IEventUpcaster>();
        private readonly string eventsNamespace;
        private readonly string eventsAssembly;

        public EventDeserializationAndVersionManager(IJsonSerializer serializer, string eventsNamespace, string eventsAssembly)
        {
            this.serializer = Ensured.NotNull(serializer, nameof(serializer));
            this.eventsNamespace = Ensured.NotEmpty(eventsNamespace, nameof(eventsNamespace));
            this.eventsAssembly = Ensured.NotEmpty(eventsAssembly, nameof(eventsAssembly));
        }

        IEventUpcasterRegistry IEventUpcasterRegistry.Register(IEventUpcaster eventUpcaster)
        {
            this.upcasters.Add(eventUpcaster.EventTypeToUpcast, eventUpcaster);
            return this;
        }

      
        public IEvent GetLatestEventVersion(string eventType, long eventSourcedVersion, long eventNumberOrNoNumberOnHydration, string payload, IEventMetadata metadata, string eventSourcedType)
        {
            var metadataDictionary = ((EventMetadata)metadata).ToDictionary();
            return this.GetLatestEventVersion(eventType, eventSourcedVersion, eventNumberOrNoNumberOnHydration, payload, metadataDictionary, eventSourcedType);
        }


        public IEvent GetLatestEventVersion(string eventType, long eventSourcedVersion, long eventNumberOrNoNumberOnHydration, string payload, string metadata, string eventSourcedType)
        {
            var metadataDictionary = this.serializer.DeserializeDictionary<string, object>(metadata);
            return this.GetLatestEventVersion(eventType, eventSourcedVersion, eventNumberOrNoNumberOnHydration, payload, metadataDictionary, eventSourcedType);
        }

        /// <remarks>
        /// This method is not a <see cref="System.Threading.Tasks.Task"/> because in normal circumstances it will 
        /// be a single threaded deserializacion process that does not require the overhead of creating a new thread.
        /// </remarks>
        private IEvent GetLatestEventVersion(string eventType, long eventSourcedVersion, long eventNumberOrNoNumberOnHydration, string payload, IDictionary<string, object> metadata, string eventSourcedType)
        {
            var metadataObject = EventMetadata.Parse(metadata, eventSourcedVersion, eventNumberOrNoNumberOnHydration, eventSourcedType);

            IEventInTransit? e;
            try
            {
                if (!this.TryResolveSystemEvent(metadataObject.EventSourcedType, eventType, payload, out e))
                    e = (IEventInTransit)this.serializer.Deserialize(payload, $"{this.eventsNamespace}.{eventType.WithFirstCharInUpper()}", this.eventsAssembly)!;

                e.SetEventMetadata(metadataObject, eventType);
            }
            catch (Exception ex)
            {
                return this.PerformUpcasting(eventType, payload, metadataObject, ex);
            }

            // Only after deserializing we realize that the event may need an Upcasting.
            return e is INeedUpcastingCheck ? this.PerformUpcasting(eventType, payload, metadataObject) : e;
        }

        private IEvent PerformUpcasting(string eventType, string payload, IEventMetadata metadataObject, Exception ex = null)
        {
            if (ex is null)
                this.logger.Verbose($"An event that may need an upcasting detected. Event type is: {eventType}. Getting latest schema version from upcaster.");
            else
                this.logger.Verbose($"Old version of event '{eventType}' detected. Getting latest schema version from upcaster. Detected from the following exception: Exception type: {ex.GetType().Name}. Exception message: {ex.Message}");

            if (!this.upcasters.TryGetValue(eventType, out var upcaster))
                throw new InvalidOperationException($"Can not find appropiate upcaster or deserializer for event of type {eventType}", ex);

            ((IEventMetadataInTransit)metadataObject).SetEventType(eventType);
            var e = upcaster.Upcast(payload, metadataObject);
            e.SetEventMetadata(metadataObject, e.GetType().Name.WithFirstCharInLower());
            return e;
        }

        private bool TryResolveSystemEvent(string eventSourcedType, string eventType, string payload, out IEventInTransit? e)
        {
            switch (eventSourcedType)
            {
                case "sequentialNumber":
                    e = this.serializer.Deserialize<NewNumberGenerated>(payload);
                    break;

                case "entityTransactionPreparation":
                    switch (eventType)
                    {
                        case "eventPrepared":
                            e = this.serializer.Deserialize<EventPrepared>(payload);
                            break;

                        case "preparedEventsBatchCleared":
                            e = this.serializer.Deserialize<PreparedEventsBatchCleared>(payload);
                            break;

                        case "entityTransactionPreparationCreated":
                            e = this.serializer.Deserialize<EntityTransactionPreparationCreated>(payload);
                            break;

                        case "lockReleaseScheduled":
                            e = this.serializer.Deserialize<LockReleaseScheduled>(payload);
                            break;

                        default:
                            throw new InvalidOperationException($"Not registered event of type {eventType}");
                    }
                    break;

                case "transactionRecord":
                    switch (eventType)
                    {
                        case "newTransactionPrepareStarted":
                            e = this.serializer.Deserialize<NewTransactionPrepareStarted>(payload);
                            break;
                        case "onlineTransactionRollbackCompleted":
                            e = this.serializer.Deserialize<OnlineTransactionRollbackCompleted>(payload);
                            break;
                        case "onlineTransactionRollbackStarted":
                            e = this.serializer.Deserialize<OnlineTransactionRollbackStarted>(payload);
                            break;
                        case "recoveredTransactionRollbackCompleted":
                            e = this.serializer.Deserialize<RecoveredTransactionRollbackCompleted>(payload);
                            break;
                        case "recoveredTransactionRollbackStarted":
                            e = this.serializer.Deserialize<RecoveredTransactionRollbackStarted>(payload);
                            break;
                        case "transactionCommitStarted":
                            e = this.serializer.Deserialize<TransactionCommitStarted>(payload);
                            break;
                        case "transactionCommitted":
                            e = this.serializer.Deserialize<TransactionCommitted>(payload);
                            break;
                        case "transactionRollforwardCompleted":
                            e = this.serializer.Deserialize<TransactionRollforwardCompleted>(payload);
                            break;
                        case "transactionRollforwardStarted":
                            e = this.serializer.Deserialize<TransactionRollforwardStarted>(payload);
                            break;
                        default:
                            throw new InvalidOperationException($"Not registered event of type {eventType}");
                    }
                    break;

                default:
                    switch (eventType)
                    {
                        case "lockAcquired":
                            e = this.serializer.Deserialize<LockAcquired>(payload);
                            break;
                        case "lockReleased":
                            e = this.serializer.Deserialize<LockReleased>(payload);
                            break;
                        default:
                            e = null;
                            return false;
                    }
                    break;
            }

            return true;
        }
    }
}
