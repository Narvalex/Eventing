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
    public class MessageOutbox : IMessageOutbox, IDisposable
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<MessageOutbox>();
        private readonly IHttpLite http;
        private readonly Func<string> tokenProvider;
        private readonly IJsonSerializer serializer;
        private readonly IPendingMessagesQueue queue;
        private bool disposed = false;
        private readonly Task thread;

        public MessageOutbox(IHttpLite httpClient, IJsonSerializer serializer, IPendingMessagesQueue queue,
            Func<string> tokenProvider = null)
        {
            Ensure.NotNull(httpClient, nameof(httpClient));
            Ensure.NotNull(serializer, nameof(serializer));
            Ensure.NotNull(queue, nameof(queue));

            this.http = httpClient;
            this.serializer = serializer;
            this.queue = queue;
            this.tokenProvider = tokenProvider is null ? () => null : tokenProvider;

            this.thread = Task.Factory.StartNew(this.SendPendingMessages, TaskCreationOptions.LongRunning);
        }

        public async Task<OutboxSendStatus> Send<T>(string uri, T message)
        {
            try
            {
                await this.http.Post<T>(uri, message, this.tokenProvider.Invoke());
                return OutboxSendStatus.Sent;
            }
            catch (ServiceUnavailableException)
            {
                this.Enqueue<T>(uri, message);
                return OutboxSendStatus.Enqueued;
            }
        }

        public async Task<IOutboxSendResult<TResult>> Send<TContent, TResult>(string uri, TContent message)
        {
            try
            {
                var result = await this.http.Post<TContent, TResult>(uri, message, this.tokenProvider.Invoke());
                return new OutboxSendResult<TResult>(OutboxSendStatus.Sent, result);
            }
            catch (ServiceUnavailableException)
            {
                this.Enqueue<TContent>(uri, message);
                return new OutboxSendResult<TResult>(OutboxSendStatus.Enqueued, default(TResult));
            }
        }

        private void Enqueue<T>(string uri, T message)
        {
            this.queue.Enqueue(new PendingMessage(uri, typeof(T).FullName, this.serializer.Serialize(message)));
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
