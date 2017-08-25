using System.Threading.Tasks;

namespace Eventing.OfflineClient
{
    public interface IOfflineClient
    {
        Task<SendStatus> Send<T>(string url, T message);
        Task<SendResult<TResult>> Send<TContent, TResult>(string url, TContent message);
    }
}
