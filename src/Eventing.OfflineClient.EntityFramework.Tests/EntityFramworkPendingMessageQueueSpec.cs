using Eventing.Client.Http;
using Eventing.Core.Serialization;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Eventing.OfflineClient.EntityFramework.Tests
{
    public class EntityFramworkPendingMessageQueueSpec
    {
        protected EntityFramworkPendingMessageQueue repository;
        protected OfflineClientBase outbox;
        protected TestableHttpClient http;
        protected Func<bool, PendingMessageQueueDbContext> contextFactory;

        public EntityFramworkPendingMessageQueueSpec()
        {
            this.contextFactory = readOnly => new PendingMessageQueueDbContext(readOnly, "TestDb");
            this.http = new TestableHttpClient();
            this.repository = new EntityFramworkPendingMessageQueue(this.contextFactory);
            this.outbox = new OfflineClientBase(this.http, new NewtonsoftJsonSerializer(TypeNameHandling.None), this.repository);
        }
    }


    public class TestableHttpClient : IHttpLite
    {
        private bool isOffline;

        public void SetOffline()
        {
            this.isOffline = true;
        }

        public int TryToSendCount { get; private set; } = 0;

        public async Task<TResult> Post<TContent, TResult>(string uri, TContent content, string token = null)
        {
            this.TryToSendCount++;
            if (this.isOffline)
            {
                throw new ServiceUnavailableException();
            }
            return await Task.FromResult<TResult>(default(TResult));
        }

        public async Task Post<TContent>(string uri, TContent content, string token = null)
        {
            await this.TryPost();
        }

        public async Task Post(string uri, string jsonContent, string token = null)
        {
            await this.TryPost();
        }

        private async Task TryPost()
        {
            this.TryToSendCount++;
            if (this.isOffline)
            {
                throw new ServiceUnavailableException();
            }
            await Task.Delay(0);
        }

        #region Unused Here
        public Task<Stream> Get(string uri, string token = null)
        {
            return Task.FromResult<Stream>(null);
        }

        public Task<TResult> Get<TResult>(string uri, string token = null)
        {
            return Task.FromResult<TResult>(default(TResult));
        }

        public Task<TResult> Post<TResult>(string uri, string jsonContent, string token = null)
        {
            return Task.FromResult<TResult>(default(TResult));
        }

        public Task Upload(string uri, Stream fileStream, string fileName, string metadatos, string token = null)
        {
            return Task.CompletedTask;
        }

        public Task<TResult> Upload<TResult>(string uri, Stream fileStream, string fileName, string metadatos, string token = null)
        {
            return Task.FromResult<TResult>(default(TResult));
        }
        #endregion
    }
}
