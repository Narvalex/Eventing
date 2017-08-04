using System.Collections.Concurrent;

namespace Eventing.OfflineClient
{
    public class InMemoryPendingMessagesQueue : IPendingMessagesQueue
    {
        private readonly ConcurrentQueue<PendingMessage> queue = new ConcurrentQueue<PendingMessage>();

        public void Dequeue()
        {
            PendingMessage result;
            this.queue.TryDequeue(out result);
        }

        public void Enqueue(PendingMessage message)
        {
            this.queue.Enqueue(message);
        }

        public bool TryPeek(out PendingMessage message)
        {
            return this.queue.TryPeek(out message);
        }
    }
}
