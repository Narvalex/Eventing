using Eventing.Client.Http;
using Eventing.Core.Serialization;
using Eventing.Log;
using System;
using System.Net.Http;
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
        private Action<Exception> onPendingError;
        private readonly IJsonSerializer serializer;
        private readonly IPendingMessagesQueue queue;
        private bool disposed = false;
        private readonly Task thread;
        private readonly int idleMilliseconds;

        public OfflineClientBase(IHttpLite httpClient, IJsonSerializer serializer, IPendingMessagesQueue queue,
            Func<string> tokenProvider = null, string prefix = "", Action<Exception> onPendingError = null, int idleMilliseconds = 1000)
        {
            Ensure.NotNull(httpClient, nameof(httpClient));
            Ensure.NotNull(serializer, nameof(serializer));
            Ensure.NotNull(queue, nameof(queue));
            Ensure.Positive(idleMilliseconds, nameof(idleMilliseconds));

            this.http = httpClient;
            this.serializer = serializer;
            this.queue = queue;
            this.tokenProvider = tokenProvider is null ? () => null : tokenProvider;
            this.onPendingError = onPendingError is null ? ex => { } : onPendingError;
            this.prefix = prefix != string.Empty ? prefix + "/" : string.Empty;
            this.idleMilliseconds = idleMilliseconds;

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
            catch (Exception ex)
            {
                if (ex is ServiceUnavailableException || ex is HttpRequestException)
                {
                    this.Enqueue<T>(uri, message);
                    return SendStatus.Enqueued;
                }

                throw;
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
            catch (Exception ex)
            {
                if (ex is ServiceUnavailableException || ex is HttpRequestException)
                {
                    this.Enqueue<TContent>(uri, message);
                    return new SendResult<TResult>(SendStatus.Enqueued, default(TResult));
                }

                throw;
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
                        if (ex.InnerException is ServiceUnavailableException || ex.InnerException is HttpRequestException)
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
                    this.log.Error(ex, $"An error ocurred while trying to send pending message. The client will continue to send messages...");
                    this.onPendingError(ex);
                    this.queue.Dequeue();
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

        private void EnterIdle()
        {
            if (this.disposed) return;
            Thread.Sleep(this.idleMilliseconds);
        }

        public void Dispose()
        {
            this.disposed = true;
            this.thread.Wait();
        }
    }
}
