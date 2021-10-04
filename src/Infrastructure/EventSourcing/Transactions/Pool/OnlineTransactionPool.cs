using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing.Transactions
{
    public class OnlineTransactionPool : IOnlineTransactionPool
    {
        private readonly ConcurrentDictionary<string, bool> transactions = new ConcurrentDictionary<string, bool>();


        public async Task AwaitWhileRegistered(string transactionId)
        {
            while (this.transactions.ContainsKey(transactionId))
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        public void Register(string transactionId) =>
            this.transactions[transactionId] = false;

        public void Unregister(string transactionId) =>
            this.transactions.TryRemove(transactionId, out var _);
    }
}
