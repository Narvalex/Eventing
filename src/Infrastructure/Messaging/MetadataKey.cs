namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// Exposes the property names of <see cref="Messaging.IEvent"/> metadata added to all 
    /// events being persisted in the store.
    /// </summary>
    public static class MetadataKey
    {
        public const string EventId = "eventId";

        public const string CorrelationId = "$correlationId";

        public const string CausationId = "$causationId";

        public const string CausationNumber = "causationNumber";

        public const string CommitId = "commitId";

        public const string Timestamp = "timestamp";

        public const string EventSourcedType = "eventSourcedType";

        public const string AuthorId = "authorId";

        public const string AuthorName = "authorName";

        public const string ClientIpAddress = "clientIpAddress";

        public const string UserAgent = "userAgent";

        public const string DisplayMode = "displayMode";

        public const string CommandTimestamp = "commandTimestamp";

        public const string PositionLatitude = "positionLatitude";

        public const string PositionLongitude = "positionLongitude";

        public const string PositionAccuracy = "positionAccuracy";

        public const string PositionAltitude = "positionAltitude";

        public const string PositionAltitudeAccuracy = "positionAltitudeAccuracy";

        public const string PositionHeading = "positionHeading";

        public const string PositionSpeed = "positionSpeed";

        public const string PositionTimestamp = "positionTimestamp";

        public const string PositionError = "positionError";
    }
}
