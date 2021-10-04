using System;
using Infrastructure.EventSourcing;
using Infrastructure.Utils;

namespace Infrastructure.Messaging
{
    public class MessageMetadata : IMessageMetadata
    {
        public MessageMetadata(string authorId, string authorName, string clientIpAddress, string userAgent, string? displayMode = null, DateTime? commandTimestamp = null,
            double? positionLatitude = null, double? positionLongitude = null, double? positionAccuracy = null, double? positionAltitude = null,
            double? positionAltitudeAccuracy = null, double? positionHeading = null, double? positionSpeed = null, DateTime? positionTimestamp = null, string? positionError = null)
        {
            this.AuthorId = Ensured.NotEmpty(authorId, nameof(authorId));
            this.AuthorName = Ensured.NotEmpty(authorName, nameof(authorName));
            this.ClientIpAddress = Ensured.NotEmpty(clientIpAddress, nameof(clientIpAddress));
            this.UserAgent = Ensured.NotEmpty(userAgent, nameof(userAgent));
            this.DisplayMode = displayMode;
            this.CommandTimestamp = commandTimestamp;
            this.PositionLatitude = positionLatitude;
            this.PositionLongitude = positionLongitude;
            this.PositionAccuracy = positionAccuracy;
            this.PositionAltitude = positionAltitude;
            this.PositionAltitudeAccuracy = positionAltitudeAccuracy;
            this.PositionHeading = positionHeading;
            this.PositionSpeed = positionSpeed;
            this.PositionTimestamp = positionTimestamp;

            if (!PositionErrorKey.IsValidPositionError(positionError))
                throw new ArgumentException("The position error code is not valid");

            this.PositionError = positionError;
        }

        public string AuthorId { get; }
        public string AuthorName { get; }
        public string ClientIpAddress { get; }
        public string UserAgent { get; }
        public string? DisplayMode { get; }
        public DateTime? CommandTimestamp { get; }
        public double? PositionLatitude { get; }
        public double? PositionLongitude { get; }
        public double? PositionAccuracy { get; }
        public double? PositionAltitude { get; }
        public double? PositionAltitudeAccuracy { get; }
        public double? PositionHeading { get; }
        public double? PositionSpeed { get; }
        public DateTime? PositionTimestamp { get; }
        public string? PositionError { get; }
    }
}
 