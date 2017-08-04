namespace Eventing.OfflineClient
{
    public interface IPendingMessagesQueue
    {
        void Enqueue(PendingMessage message);
        bool TryPeek(out PendingMessage message);
        void Dequeue();
    }
}
