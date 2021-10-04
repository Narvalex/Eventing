using Infrastructure.Messaging;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing.Transactions
{
    public interface IOnlineTransaction : IDisposable
    {
        string TransactionId { get; }
        Task<T> AcquireLockAsync<T>(string id) where T : class, IEventSourced;
        Task<T> New<T>(string id) where T : class, IEventSourced;
        Task PrepareAsync(IEventSourced eventSourced);
        Task CommitAsync();
        Task Rollback();
    }
}
