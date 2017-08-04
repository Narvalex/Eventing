using Eventing.Client.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Eventing.OfflineClient.Tests
{
    public class MessageOutboxSpec
    {
        public MessageOutboxSpec()
        {
            this.HttpClient = new TestableHttpClient();
        }

        public TestableHttpClient HttpClient { get; }
    }

    public class LoginDto
    {
        public string User { get; set; }
        public string Password { get; set; }
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
            throw new NotImplementedException();
        }

        public Task<TResult> Get<TResult>(string uri, string token = null)
        {
            throw new NotImplementedException();
        }

        public Task<TResult> Post<TResult>(string uri, string jsonContent, string token = null)
        {
            throw new NotImplementedException();
        }

        public Task Upload(string uri, Stream fileStream, string fileName, string metadatos, string token = null)
        {
            throw new NotImplementedException();
        }

        public Task<TResult> Upload<TResult>(string uri, Stream fileStream, string fileName, string metadatos, string token = null)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
