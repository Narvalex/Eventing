using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.EntityFramework.EventLog
{
    public class EventEntity
    {
        public long EventNumber { get; set; }
        public Guid EventId { get; set; }
        public string CorrelationId { get; set; }
        public string CausationId { get; set; }
        public string CommitId { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventSourcedType { get; set; }
        // El id del event sourced entity. Not in metadata
        public string StreamId { get; set; }
        public long EventSourcedVersion { get; set; }
        public long? CausationNumber { get; set; }
        public string EventType { get; set; }
        // Not in metadata
        public string Payload { get; set; }
        // The commit position of the record. Not in metadata
        public long CommitPosition { get; set; }
        // The prepare position of the record. Not in metadata
        public long PreparePosition { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string ClientIpAddress { get; set; }
        public string UserAgent { get; set; }
        public string? DisplayMode { get; set; }
        public DateTime? CommandTimestamp { get; set; }
        public double? PositionLatitude { get; set; }
        public double? PositionLongitude { get; set; }
        public double? PositionAccuracy { get; set; }
        public double? PositionAltitude { get; set; }
        public double? PositionAltitudeAccuracy { get; set; }
        public double? PositionHeading { get; set; }
        public double? PositionSpeed { get; set; }
        public DateTime? PositionTimestamp { get; set; }
        public string? PositionError { get; set; }
    }

    public class EventEntityConfig : IEntityTypeConfiguration<EventEntity>
    {
        public void Configure(EntityTypeBuilder<EventEntity> builder)
        {
            builder.HasKey(x => x.EventNumber);
            builder.Property(x => x.EventNumber).ValueGeneratedNever();
            builder.HasIndex(x => new { x.EventSourcedType, x.EventSourcedVersion }).IsClustered(false);
            builder.HasIndex(x => new { x.EventSourcedType, x.StreamId }).IsClustered(false);
            builder.ToTable("Events");
        }
    }
}
