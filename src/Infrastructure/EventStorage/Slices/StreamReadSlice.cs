namespace Infrastructure.EventStorage
{
    public class StreamReadSlice
    {
        public StreamReadSlice(SliceFetchStatus status, long nextEventNumber, bool isEndOfStream)
        {
            this.Status = status;
            this.NextEventNumber = nextEventNumber;
            this.IsEndOfStream = isEndOfStream;
        }

        /// <summary>
        /// The <see cref="SliceFetchStatus"/> representing the status of this read attempt.
        /// </summary>
        public SliceFetchStatus Status { get; }
        /// <summary>
        /// The next event number that can be read.
        /// </summary>
        public long NextEventNumber { get; }

        /// <summary>
        /// A boolean representing whether or not this is the end of the stream.
        /// </summary>
        public bool IsEndOfStream { get; }
    }
}
