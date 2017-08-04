using System.Threading.Tasks;

namespace Eventing.OfflineClient
{
    public interface IMessageOutbox
    {
        Task<OutboxSendStatus> Send<T>(string url, T message);
        Task<IOutboxSendResult<TResult>> Send<TContent, TResult>(string url, TContent message);
    }

    public enum OutboxSendStatus
    {
        Enqueued,
        Sent
    }

    public interface IOutboxSendResult<T>
    {
        OutboxSendStatus Status { get; }
        T Result { get; }
    }
}
