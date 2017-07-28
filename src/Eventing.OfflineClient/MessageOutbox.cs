using Eventing.Client.Http;
using System;
using System.Threading.Tasks;

namespace Eventing.OfflineClient
{
    public class MessageOutbox : IMessageOutbox
    {
        private readonly IHttpLite http;

        public MessageOutbox(IHttpLite httpClient)
        {
            Ensure.NotNull(httpClient, nameof(httpClient));

            this.http = httpClient;
        }

        public Task<OutboxSendStatus> Send<T>(object message)
        {
            throw new NotImplementedException();
        }

        public Task<IOutboxSendResult<TResult>> Send<TContent, TResult>(object message)
        {
            throw new NotImplementedException();
        }
    }
}
