namespace Eventing.OfflineClient
{
    /// <summary>
    /// A queue. It may not be async since this will be only used by a single thread in the background
    /// </summary>
    public interface IPendingMessagesQueue
    {
        /// <summary>
        /// Enqueues the pending message.
        /// </summary>
        /// <param name="message">The pending message.</param>
        void Enqueue(PendingMessage message);

        /// <summary>
        /// Peeks the first message in the queue.
        /// </summary>
        /// <param name="message">The first message in the queue.</param>
        /// <returns>True if there was a message in the queue. False if the queue is empty.</returns>
        bool TryPeek(out PendingMessage message);

        /// <summary>
        /// Removes the first message in the queue.
        /// </summary>
        void Dequeue();
    }
}
