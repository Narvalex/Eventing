using System.Threading.Tasks;

namespace Infrastructure.EventSourcing.Transactions
{
    public interface IOnlineTransactionPool
    {
        void Register(string transactionId);
        void Unregister(string transactionId);
        Task AwaitWhileRegistered(string transactionId);
    }
}
