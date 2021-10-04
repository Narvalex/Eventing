namespace Infrastructure.Snapshotting
{
    public class SnapshotSchema
    {
        public SnapshotSchema(string type, string assembly, int version, string hash, bool thereAreStaleSnapshots)
        {
            this.Type = type;
            this.Assembly = assembly;
            this.Version = version;
            this.Hash = hash;
            this.ThereAreStaleSnapshots = thereAreStaleSnapshots;
        }

        public string Type { get; }
        public string Assembly { get; }
        public int Version { get; private set; }
        public string Hash { get; private set; }
        public bool ThereAreStaleSnapshots { get; private set; }

        public SnapshotSchema UpdateSchema(string hash)
        {
            this.Hash = hash;
            this.Version += 1;
            this.ThereAreStaleSnapshots = true;
            return this;
        }

        public SnapshotSchema NotifyAllSnapshotsAreUpToDate()
        {
            this.ThereAreStaleSnapshots = false;
            return this;
        }
    }
}
