namespace Infrastructure.Configuration
{
    public class EmbeddedEventStore
    {
        public string Ip { get; set; }
        public int IntTcpPort { get; set; }
        public int ExtTcpPort { get; set; }
        public int ExtHttpPort { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int IntTcpHeartbeatTimeout { get; set; }
        public int ExtTcpHeartbeatTimeout { get; set; }
        public bool ShowLog { get; set; }
    }
}
