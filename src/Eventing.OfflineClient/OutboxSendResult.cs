namespace Eventing.OfflineClient
{
    public class OutboxSendResult<T> : IOutboxSendResult<T>
    {
        public OutboxSendResult(OutboxSendStatus status, T result)
        {
            this.Status = status;
            this.Result = result;
        }

        public OutboxSendStatus Status { get; }

        public T Result { get; }
    }
}
