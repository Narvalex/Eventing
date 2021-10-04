namespace Infrastructure.Configuration
{
    public class DevelopmentConfig
    {
        public bool EnableMasterKey { get; set; }
        public string MasterKey { get; set; }
        public string OLTP { get; set; }
        public bool ConnectToIngres { get; set; }
        public bool ServeReactProductionBuild { get; set; }
        public EmbeddedEventStore EmbeddedEventStore { get; set; }
        public string SqlEventStore { get; set; }
    }
}
