using Infrastructure.EventSourcing;
using System;
using System.Collections.Generic;

namespace Infrastructure.Messaging.Handling
{
    public struct Checkpoint : IEquatable<Checkpoint>
    {
        public Checkpoint(EventPosition eventPosition, long eventNumber)
        {
            if (eventNumber < EventStream.NoEventsNumber)
                throw new ArgumentException("The event number cannot be less than -1", 
                    nameof(eventNumber));

            this.EventPosition = eventPosition;
            this.EventNumber = eventNumber;
        }

        public EventPosition EventPosition { get; }

        public long EventNumber { get; }

        // We do not put he $all event number because in future event store versions it will be filtered out 
        // from the database itself on subscripcion receiving.

        public static readonly Checkpoint Start = new Checkpoint(EventPosition.Start, EventStream.NoEventsNumber);

        public override bool Equals(object? obj)
        {
            return obj is Checkpoint checkpoint && this.Equals(checkpoint);
        }

        public bool Equals(Checkpoint other)
        {
            return EqualityComparer<EventPosition>.Default.Equals(this.EventPosition, other.EventPosition) &&
                   this.EventNumber == other.EventNumber;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.EventPosition, this.EventNumber);
        }

        public static bool operator ==(Checkpoint left, Checkpoint right) => left.Equals(right);
        public static bool operator !=(Checkpoint left, Checkpoint right) => !(left == right);

        public static bool operator >(Checkpoint left, Checkpoint right) => left.EventNumber > right.EventNumber;
        public static bool operator <(Checkpoint left, Checkpoint right) => left.EventNumber < right.EventNumber;
    }
}
