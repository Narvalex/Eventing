using System;

namespace Infrastructure.Messaging
{
    public interface IMessageMetadata
    {
        string AuthorId { get; }
        string AuthorName { get; }
        string ClientIpAddress { get; }
        string UserAgent { get; }
        string? DisplayMode { get; }

        /// <summary>
        /// Represents the command timestamp, if applicable. Useful to check SLA
        /// </summary>
        public DateTime? CommandTimestamp { get; }

        /// <summary>
        /// Represents the latitude of the position in decimal degrees.
        /// </summary>
        public double? PositionLatitude { get; }

        /// <summary>
        /// Represents the longitude of a geographical position, specified in decimal degrees
        /// </summary>
        public double? PositionLongitude { get; }

        /// <summary>
        /// A strictly positive double representing the accuracy, with a 95% confidence level, 
        /// of the <see cref="IMessageMetadata.PositionLatitude"/> and <see cref="IMessageMetadata.PositionLongitude"/> 
        /// properties expressed in meters.
        /// </summary>
        public double? PositionAccuracy { get; }

        /// <summary>
        /// A double representing the altitude of the position in meters, relative to sea level. 
        /// This value is null if the implementation cannot provide this data.
        /// </summary>
        public double? PositionAltitude { get; }

        /// <summary>
        /// A strictly positive double representing the accuracy, with a 95% confidence level, of 
        /// the altitude expressed in meters. This value is null if the implementation doesn't support measuring altitude.
        /// </summary>
        public double? PositionAltitudeAccuracy { get; }

        /// <summary>
        /// is a double representing the direction in which the device is traveling. This value, specified in degrees, indicates how 
        /// far off from heading due north the device is. Zero degrees represents true true north, and the direction is determined 
        /// clockwise (which means that east is 90 degrees and west is 270 degrees). If Coordinates.speed is 0, heading is NaN. 
        /// If the device is not able to provide heading information, this value is null.
        /// </summary>
        public double? PositionHeading { get; }

        /// <summary>
        /// A double representing the velocity of the device in meters per second. This value is null if the implementation is not able to measure it.
        /// </summary>
        public double? PositionSpeed { get; }

        /// <summary>
        /// Represents the date and the time of the creation of the Position object it belongs to. The precision is to the millisecond.
        /// </summary>
        public DateTime? PositionTimestamp { get; }

        public string? PositionError { get; }
    }
}