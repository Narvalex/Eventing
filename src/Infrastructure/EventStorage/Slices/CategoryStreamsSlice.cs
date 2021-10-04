using System.Collections.Generic;

namespace Infrastructure.EventStorage
{
    public class CategoryStreamsSlice : StreamReadSlice
    {
        public CategoryStreamsSlice(SliceFetchStatus status, List<StreamNameAndVersion> streams, long nexEventNumber, bool isEndOfStream) 
            : base(status, nexEventNumber, isEndOfStream)
        {
            this.Streams = streams;
        }

        /// <summary>
        /// The stream names that belongs to a category.
        /// </summary>
        public List<StreamNameAndVersion> Streams { get; }
    }

    public class StreamNameAndVersion
    {
        public StreamNameAndVersion(string streamName, long version)
        {
            this.StreamName = streamName;
            this.Version = version;
        }

        public string StreamName { get; }

        // If no limit is set then EventStream.NoEventsNumber
        public long Version { get; }
    }
}
