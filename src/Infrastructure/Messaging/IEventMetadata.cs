using Infrastructure.EventSourcing;
using Infrastructure.Messaging.Handling;
using System;
using System.Collections.Generic;

namespace Infrastructure.Messaging
{
    public interface IEventMetadata : IMessageMetadata
    {
        string CausationId { get; }
        string CommitId { get; }
        string CorrelationId { get; }
        Guid EventId { get; }
        long EventNumber { get; }
        long? CausationNumber { get; }
        string EventSourcedType { get; }
        long EventSourcedVersion { get; }
        DateTime Timestamp { get; }
        string EventType { get; }
        IDictionary<string, object> ToDictionary();

        // Avoid serialization. Only used in subscription, for readmodeling
        Checkpoint GetCheckpoint();
        void SetCheckpoint(Checkpoint checkpoint);

        bool ResolveIfEventNumberIsValid() => this.EventNumber != EventStream.NoEventsNumber;
    }

    public interface IEventMetadataInTransit : IEventMetadata
    {
        void SetEventType(string eventType);
    }
}