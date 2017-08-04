namespace Eventing.OfflineClient
{
    public class PendingMessage
    {
        public PendingMessage(string url, string payload)
        {
            this.Url = url;
            this.Payload = payload;
        }

        public string Url { get; }
        public string Payload { get; }
    }
}
