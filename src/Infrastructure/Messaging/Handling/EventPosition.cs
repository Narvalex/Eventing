using System;

namespace Infrastructure.Messaging.Handling
{
	/// <summary>
	/// A structure referring to a potential logical record position
	/// in the Event Store transaction file.
	/// </summary>
	public struct EventPosition : IEquatable<EventPosition>
	{
		/// <summary>
		/// Constructs a position with the given commit and prepare positions.
		/// It is not guaranteed that the position is actually the start of a
		/// record in the transaction file.
		/// 
		/// The commit position cannot be less than the prepare position.
		/// </summary>
		/// <param name="commitPosition">The commit position of the record.</param>
		/// <param name="preparePosition">The prepare position of the record.</param>
		public EventPosition(long commitPosition, long preparePosition)
		{
			if (commitPosition < preparePosition)
				throw new ArgumentException("The commit position cannot be less than the prepare position",
					"commitPosition");

			this.CommitPosition = commitPosition;
			this.PreparePosition = preparePosition;
		}

		/// <summary>
		/// The commit position of the record
		/// </summary>
		public long CommitPosition { get; }

		/// <summary>
		/// The prepare position of the record.
		/// </summary>
		public long PreparePosition { get; }

		/// <summary>
		/// Position representing the start of the transaction file
		/// </summary>
		public static readonly EventPosition Start = new EventPosition(0, 0);

		/// <summary>
		/// Position representing the end of the transaction file
		/// </summary>
		public static readonly EventPosition End = new EventPosition(-1, -1);

		#region Equality checks
		public override bool Equals(object? obj)
		{
			return obj is EventPosition position && this.Equals(position);
		}

		public bool Equals(EventPosition other)
		{
			return this.PreparePosition == other.PreparePosition &&
				   this.CommitPosition == other.CommitPosition;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.PreparePosition, this.CommitPosition);
		}

		public static bool operator ==(EventPosition left, EventPosition right) => left.Equals(right);
		public static bool operator !=(EventPosition left, EventPosition right) => !(left == right); 
		#endregion
	}
}
