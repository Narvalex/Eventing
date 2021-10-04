namespace Infrastructure.Configuration
{
    public class EventStoreConnectionInfo
    {
        public string Ip { get; set; }
        public int TcpPort { get; set; }
        public int HttpPort { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
