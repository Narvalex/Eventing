namespace Infrastructure.Configuration
{
    public class Settings
    {
        public bool VerboseLogLevel { get; set; }
        public bool EnableReadModelGeneration { get; set; }
        public bool EnableEventProcessing { get; set; }
        public int SnapshotTimeToLiveInMinutes { get; set; }
        public int SnapshottingInterval { get; set; }
        public long SnapshotsInMemoryCacheSizeLimitInBytes { get; set; }
        public int EventProcessingRetryIntervalInSecs { get; set; }
        public bool EnableParallelismThrottling { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public int MinDegreeOfParallelism { get; set; }
        public int BatchReadModelProjectionIntervalInSecs { get; set; }
        public int MinimunBatchReadModelProjectionIntervalInMillisecs { get; set; }
        public int ReadModelCacheExpirationInSeconds { get; set; }
    }
}
