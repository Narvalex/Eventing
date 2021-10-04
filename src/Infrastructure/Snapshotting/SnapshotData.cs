using System;

namespace Infrastructure.Snapshotting
{
    public class SnapshotData
    {
        private int? schemaVersion;

        public SnapshotData(string streamName, long version, string payload, string type, string assembly, long size, int? schemaVersion = null)
        {
            this.StreamName = streamName;
            this.Version = version;
            this.Payload = payload;
            this.Type = type;
            this.Assembly = assembly;
            this.Size = size;
            this.schemaVersion = schemaVersion;
        }

        public string StreamName { get; }
        public long Version { get; }
        public string Payload { get; }
        public string Type { get; }
        public string Assembly { get; }
        public long Size { get; }

        public int SchemaVersion
        {
            get
            {
                if (!this.schemaVersion.HasValue)
                    throw new InvalidOperationException("Schema was not yet loaded");

                return this.schemaVersion.Value;
            }
            internal set => this.schemaVersion = value;
        }
    }
}
