using System.Threading.Tasks;

namespace Eventing.OfflineClient
{
    public interface IMessageOutbox
    {
        Task<OutboxSendStatus> Send<T>(object message);
        Task<IOutboxSendResult<TResult>> Send<TContent, TResult>(object message);
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
