using Infrastructure.EventSourcing;
using Infrastructure.Messaging.Handling;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;

namespace Infrastructure.Messaging
{
    /// <summary>
    /// Exposes the property names of standard metadata added to all 
    /// events being persisted in the store.
    /// </summary>
    public class EventMetadata : MessageMetadata, IEventMetadata, IEventMetadataInTransit
    {
        private Checkpoint checkpoint;

        public EventMetadata(Guid eventId, string correlationId, string causationId,
            string commitId, DateTime timestamp,
            string authorId, string authorName, string clientIpAddress, string userAgent, long? causationNumber = null, string? displayMode = null, DateTime? commandTimestamp = null,
            double? positionLatitude = null, double? positionLongitude = null, double? positionAccuracy = null, double? positionAltitude = null,
            double? positionAltitudeAccuracy = null, double? positionHeading = null, double? positionSpeed = null, DateTime? positionTimestamp = null,
            string? positionError = null)
            : base(authorId, authorName, clientIpAddress, userAgent, displayMode, commandTimestamp, positionLatitude, positionLongitude, positionAccuracy, positionAltitude,
            positionAltitudeAccuracy, positionHeading, positionSpeed, positionTimestamp, positionError)
        {
#if DEBUG
            this.EventId = Ensured.NotDefault(eventId, nameof(eventId));
            this.CorrelationId = Ensured.NotEmpty(correlationId, nameof(correlationId));
            this.CausationId = Ensured.NotEmpty(causationId, nameof(causationId));
            this.CommitId = Ensured.NotEmpty(commitId, nameof(commitId));
            this.Timestamp = Ensured.NotDefault(timestamp, nameof(timestamp));

#else
            this.EventId = eventId;
            this.CorrelationId = correlationId;
            this.CausationId = causationId;
            this.CommitId = commitId;
            this.Timestamp = timestamp;

#endif
            this.CausationNumber = causationNumber;
        }

        /// <summary>
        /// The event id.
        /// </summary>
        public Guid EventId { get; }

        /// <summary>
        /// Correlates an event to a process with a common identifier.
        /// </summary>
        public string CorrelationId { get; }

        /// <summary>
        /// More info on causation id: https://blog.arkency.com/correlation-id-and-causation-id-in-evented-systems/
        /// </summary>
        public string CausationId { get; }

        /// <summary>
        /// All the events that where persisted in a transaction shares this identifier.
        /// </summary>
        public string CommitId { get; }

        /// <summary>
        /// The publish date and time.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// This full CLR type name of the event sourced entity. This helps to build snapshots for a given type.
        /// </summary>
        public string EventSourcedType { get; private set; }

        /// <summary>
        /// The event sourced entity version when the event where commited. This is usually added when 
        /// receiving events from a subscription. This is not included before committing in 
        /// <see cref="EventSourcedRepository"/> since there are events that are 
        /// appended without checking version.
        /// </summary>
        public long EventSourcedVersion { get; private set; }

        /// <summary>
        /// The event number from the stream perspective. This is useful to save checkpoints when subscribing. This is not 
        /// included in <see cref="EventSourcedRepository"/>. 
        /// When resolving from <see cref="EventStorage. IEventStore"/> it will allways be the same as the version. 
        /// And when resolving from <see cref="Handling.IEventSubscription"/> it will always be the event 
        /// number of the subscribed stream.
        /// We do not include the <see cref="Handling.Checkpoint"/> since the EventNumber is enough to find in EventStore and SQL Server
        /// </summary>
        public long EventNumber { get; private set; }

        /// <summary>
        /// If the event is caused by another event, then the causation number is relative to the subscribed stream position.
        /// This is used in <see cref="EventSourced"/> to avoid duplicate event handling.
        /// </summary>
        public long? CausationNumber { get; private set; }

        /// <summary>
        /// The event type is the name that identifies by a string the payload schema.
        /// </summary>
        public string EventType { get; private set; }

        void IEventMetadataInTransit.SetEventType(string eventType) => this.EventType = eventType;

        public static IEventMetadata Parse(IDictionary<string, object> dictionary, long eventSourcedVersion, long eventNumber, string eventSourcedType)
        {
            var metadata = new EventMetadata(
                Guid.Parse(dictionary[MetadataKey.EventId].ToString()),
                (string)dictionary[MetadataKey.CorrelationId],
                (string)dictionary[MetadataKey.CausationId],
                (string)dictionary[MetadataKey.CommitId],
                (DateTime)dictionary[MetadataKey.Timestamp],
                (string)dictionary[MetadataKey.AuthorId],
                (string)dictionary[MetadataKey.AuthorName],
                (string)dictionary[MetadataKey.ClientIpAddress],
                (string)dictionary[MetadataKey.UserAgent],
                dictionary.TryGetValue(MetadataKey.CausationNumber) as long?,
                dictionary.TryGetValue(MetadataKey.DisplayMode) as string,
                dictionary.TryGetValue(MetadataKey.CommandTimestamp) as DateTime?,
                dictionary.TryGetValue(MetadataKey.PositionLatitude) as double?,
                dictionary.TryGetValue(MetadataKey.PositionLongitude) as double?,
                dictionary.TryGetValue(MetadataKey.PositionAccuracy) as double?,
                dictionary.TryGetValue(MetadataKey.PositionAltitude) as double?,
                dictionary.TryGetValue(MetadataKey.PositionAltitudeAccuracy) as double?,
                dictionary.TryGetValue(MetadataKey.PositionHeading) as double?,
                dictionary.TryGetValue(MetadataKey.PositionSpeed) as double?,
                dictionary.TryGetValue(MetadataKey.PositionTimestamp) as DateTime?,
                dictionary.TryGetValue(MetadataKey.PositionError) as string);

            metadata.EventSourcedVersion = eventSourcedVersion;
            metadata.EventNumber = eventNumber;
            metadata.EventSourcedType = eventSourcedType;

            return metadata;
        }

        public IDictionary<string, object> ToDictionary()
        {
            var d = new Dictionary<string, object>
            {
                { MetadataKey.EventId, this.EventId },
                { MetadataKey.CorrelationId, this.CorrelationId },
                { MetadataKey.CausationId, this.CausationId }
            };

            if (this.CausationNumber.HasValue)
                d[MetadataKey.CausationNumber] = this.CausationNumber;

            d[MetadataKey.CommitId] = this.CommitId;
            d[MetadataKey.Timestamp] = this.Timestamp;
            d[MetadataKey.AuthorId] = this.AuthorId;
            d[MetadataKey.AuthorName] = this.AuthorName;
            d[MetadataKey.ClientIpAddress] = this.ClientIpAddress;
            d[MetadataKey.UserAgent] = this.UserAgent;


            if (this.DisplayMode != null)
                d[MetadataKey.DisplayMode] = this.DisplayMode;
            if (this.CommandTimestamp is { })
                d[MetadataKey.CommandTimestamp] = this.CommandTimestamp;
            if (this.PositionLatitude is { })
                d[MetadataKey.PositionLatitude] = this.PositionLatitude;
            if (this.PositionLongitude is { })
                d[MetadataKey.PositionLongitude] = this.PositionLongitude;
            if (this.PositionAccuracy is { })
                d[MetadataKey.PositionAccuracy] = this.PositionAccuracy;
            if (this.PositionAltitude is { })
                d[MetadataKey.PositionAltitude] = this.PositionAltitude;
            if (this.PositionAltitudeAccuracy is { })
                d[MetadataKey.PositionAltitudeAccuracy] = this.PositionAltitudeAccuracy;
            if (this.PositionHeading is { })
                d[MetadataKey.PositionHeading] = this.PositionHeading;
            if (this.PositionSpeed is { })
                d[MetadataKey.PositionSpeed] = this.PositionSpeed;
            if (this.PositionTimestamp is { })
                d[MetadataKey.PositionTimestamp] = this.PositionTimestamp;
            if (this.PositionError is { })
                d[MetadataKey.PositionError] = this.PositionError;

            return d;
        }

        public Checkpoint GetCheckpoint() => 
            this.checkpoint;

        public void SetCheckpoint(Checkpoint checkpoint) => 
            this.checkpoint = checkpoint;

        public void SetOfEventNumberForTestOnly(long eventNumber) => this.EventNumber = eventNumber;
        public void SetCausationNumberUnsafe(long causationNumber) => this.CausationNumber = causationNumber;
        public void SetEventSourcedTypeUnsafe(string eventSourcedType) => this.EventSourcedType = eventSourcedType;
    }
}
