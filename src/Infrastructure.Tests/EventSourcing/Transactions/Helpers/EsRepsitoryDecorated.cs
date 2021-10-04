using Infrastructure.EventSourcing;
using Infrastructure.EventSourcing.Transactions;
using Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Tests.Transactions.Helpers
{
    public class EsRepsitoryDecorated : IEventSourcedRepository
    {
        private readonly IEventSourcedRepository repo;
        private IOnlineTransaction tx = null!;

        public EsRepsitoryDecorated(IEventSourcedRepository repo)
        {
            this.repo = repo;
        }

        public IOnlineTransaction Transaction => this.tx;

        public Task AppendAsync(Type type, IEnumerable<IEventInTransit> events, IMessageMetadata incomingMetadata, string correlationId, string causationId)
        {
            return this.repo.AppendAsync(type, events, incomingMetadata, correlationId, causationId);
        }

        public Task CommitAsync(IEventSourced eventSourced)
        {
            return this.repo.CommitAsync(eventSourced);
        }

        public Task<bool> ExistsAsync(Type type, string streamName)
        {
            return this.repo.ExistsAsync(type, streamName);
        }

        public Task<IEventSourced?> GetByStreamNameAsync(Type type, string streamName)
        {
            return this.repo.GetByStreamNameAsync(type, streamName);
        }

        public Task<IEventSourced?> TryGetByStreamNameEvenIfDoesNotExistsAsync(Type type, string streamName)
        {
            return this.repo.TryGetByStreamNameEvenIfDoesNotExistsAsync(type, streamName);
        }

        public Task<string> GetLastEventSourcedId<T>(int offset = 0)
        {
            return this.repo.GetLastEventSourcedId<T>(offset);
        }

        public async Task<IOnlineTransaction> NewTransaction(string correlationId, string causationId, long? causationNumber, IMessageMetadata metadata, bool isCommandMetadata)
        {
            this.tx = await this.repo.NewTransaction(correlationId, causationId, causationNumber, metadata, isCommandMetadata);
            return this.tx;
        }

        Task<bool> IEventSourcedReader.ExistsAsync<T>(string streamName)
        {
            return this.repo.ExistsAsync<T>(streamName);
        }

        IAsyncEnumerable<T> IEventSourcedReader.GetAsAsyncStream<T>()
        {
            return this.repo.GetAsAsyncStream<T>();
        }

        IAsyncEnumerable<T> IEventSourcedReader.GetAsAsyncStream<T>(IEvent contextLimitEvent)
        {
            return this.repo.GetAsAsyncStream<T>(contextLimitEvent);
        }

        public Task<T?> GetByStreamNameAsync<T>(string streamName) where T : class, IEventSourced
        {
            return this.repo.GetByStreamNameAsync<T>(streamName);
        }

        public Task<T?> GetByStreamNameAsync<T>(string streamName, long maxVersion) where T : class, IEventSourced
        {
            return ((IEventSourcedReader)this.repo).GetByStreamNameAsync<T>(streamName, maxVersion);
        }

        public Task<T> TryGetByStreamNameEvenIfDoesNotExistsAsync<T>(string streamName) where T : class, IEventSourced
        {
            return this.repo.TryGetByStreamNameEvenIfDoesNotExistsAsync<T>(streamName);
        }

        public Task AwaitUntilTransactionGoesOffline(string transactionId) => this.repo.AwaitUntilTransactionGoesOffline(transactionId);
    }
}
