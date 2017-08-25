using Eventing.Client.Http;
using Eventing.Core.Serialization;
using Eventing.Log;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eventing.OfflineClient
{
    /// <summary>
    /// Message outbox. The recipient must be idempotent of all messages sent wint this.
    /// </summary>
    public class OfflineClientBase : IOfflineClient, IDisposable
    {
        private readonly string prefix;
        private readonly ILogLite log = LogManager.GetLoggerFor<OfflineClientBase>();
        private readonly IHttpLite http;
        private Func<string> tokenProvider;
        private readonly IJsonSerializer serializer;
        private readonly IPendingMessagesQueue queue;
        private bool disposed = false;
        private readonly Task thread;

        public OfflineClientBase(IHttpLite httpClient, IJsonSerializer serializer, IPendingMessagesQueue queue,
            Func<string> tokenProvider = null, string prefix = "")
        {
            Ensure.NotNull(httpClient, nameof(httpClient));
            Ensure.NotNull(serializer, nameof(serializer));
            Ensure.NotNull(queue, nameof(queue));

            this.http = httpClient;
            this.serializer = serializer;
            this.queue = queue;
            this.tokenProvider = tokenProvider is null ? () => null : tokenProvider;
            this.prefix = prefix != string.Empty ? prefix + "/" : string.Empty;

            this.thread = Task.Factory.StartNew(this.SendPendingMessages, TaskCreationOptions.LongRunning);
        }

        public async Task<SendStatus> Send<T>(string uri, T message)
        {
            uri = this.BuildUri(uri);
            try
            {
                await this.http.Post<T>(uri, message, this.tokenProvider.Invoke());
                return SendStatus.Sent;
            }
            catch (ServiceUnavailableException)
            {
                this.Enqueue<T>(uri, message);
                return SendStatus.Enqueued;
            }
        }

        public async Task<SendResult<TResult>> Send<TContent, TResult>(string uri, TContent message)
        {
            uri = this.BuildUri(uri);
            try
            {
                var result = await this.http.Post<TContent, TResult>(uri, message, this.tokenProvider.Invoke());
                return new SendResult<TResult>(SendStatus.Sent, result);
            }
            catch (ServiceUnavailableException)
            {
                this.Enqueue<TContent>(uri, message);
                return new SendResult<TResult>(SendStatus.Enqueued, default(TResult));
            }
        }

        private void Enqueue<T>(string uri, T message)
        {
            this.queue.Enqueue(new PendingMessage(uri, typeof(T).Name, this.serializer.Serialize(message)));
        }

        private void SendPendingMessages()
        {
            while (!this.disposed)
            {
                PendingMessage pending;
                if (!this.queue.TryPeek(out pending))
                    this.EnterIdle();
                else
                {
                    try
                    {
                        this.http.Post(pending.Url, pending.Payload).Wait();
                        this.queue.Dequeue();
                    }
                    catch (AggregateException ex)
                    {
                        if (ex.InnerException is ServiceUnavailableException)
                        {
                            this.EnterIdle();
                            continue;
                        }
                        else
                        {
                            enterError(ex);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        enterError(ex);
                        continue;
                    }
                }

                void enterError(Exception ex)
                {
                    var seconds = 30;
                    this.log.Error(ex, $"An error ocurred while trying to send pending message. Retry in {seconds} seconds...");
                    this.EnterIdle(TimeSpan.FromSeconds(seconds));
                }
            }
        }

        public void SetupTokenProvider(Func<string> tokenProvider)
        {
            // On the fly
            this.tokenProvider = tokenProvider;
        }

        private string BuildUri(string uri)
        {
            return this.prefix + uri;
        }

        private void EnterIdle() => this.EnterIdle(TimeSpan.FromMilliseconds(100));
        private void EnterIdle(TimeSpan timeSpan)
        {
            if (this.disposed) return;
            Thread.Sleep(timeSpan);
        }

        public void Dispose()
        {
            this.disposed = true;
            this.thread.Wait();
        }
    }
}
