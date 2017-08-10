namespace Eventing.OfflineClient
{
    public class PendingMessage
    {
        public PendingMessage(string url, string type, string payload)
        {
            this.Url = url;
            this.Type = type;
            this.Payload = payload;
        }

        public string Url { get; }
        public string Type { get; }
        public string Payload { get; }
    }
}
