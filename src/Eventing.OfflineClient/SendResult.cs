namespace Eventing.OfflineClient
{
    public class SendResult<T>
    {
        public SendResult(SendStatus status, T result)
        {
            this.Status = status;
            this.Result = result;
        }

        public SendStatus Status { get; }

        public T Result { get; }
    }
}
