namespace Infrastructure.Configuration.App
{
    public class BackupConfig
    {
        public bool Enabled { get; set; }
        public string EventStoreOriginPath { get; set; } = null!;
        public string DestinationPath { get; set; } = null!;
        public string RestorePath { get; set; } = null!;
        public int Hour { get; set; }
        public int Minutes { get; set; }
        public int BackupWindowInHours { get; set; }
        public int MaxBackupFiles { get; set; }
        public string? EmailNotification { get; set; }
    }
}
