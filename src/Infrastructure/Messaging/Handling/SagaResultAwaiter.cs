using Infrastructure.EventSourcing;
using Infrastructure.Logging;
using Infrastructure.Utils;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public class SagaResultAwaiter : ISagaResultAwaiter
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<SagaResultAwaiter>();
        private readonly IEventSourcedReader reader;
        private readonly TimeSpan pollingInterval = TimeSpan.FromMilliseconds(10);
        private readonly TimeSpan maxInterval = TimeSpan.FromMilliseconds(100);
        private readonly TimeSpan timeToWait = TimeSpan.FromSeconds(60);

        public SagaResultAwaiter(IEventSourcedReader reader)
        {
            this.reader = Ensured.NotNull(reader, nameof(reader));
        }

        public async Task<IHandlingResult> Await<T>(string streamId, Func<T?, IHandlingResult?> awaitLogic) where T : class, IEventSourced
        {
            var sagaName = typeof(T).Name;
            this.log.Verbose($"Awaiting {sagaName} saga outcome for {this.timeToWait.TotalSeconds} seconds...");

            var result = await TaskRetryFactory.StartPollingAsync(
                async () => awaitLogic(await this.reader.TryGetByIdAsync<T>(streamId)),
                result => result != null,
                this.pollingInterval,
                this.timeToWait,
                this.maxInterval
            );

            this.log.Verbose($"{sagaName} saga awaiting ended successfully");
            return result!;
        }

        public async Task<IHandlingResult> Await<T>(string streamId, Func<T?, Task<IHandlingResult?>> awaitLogic) where T : class, IEventSourced
        {
            var sagaName = typeof(T).Name;
            this.log.Verbose($"Awaiting {sagaName} saga outcome for {this.timeToWait.TotalSeconds} seconds...");

            var result = await TaskRetryFactory.StartPollingAsync(
                async () => await awaitLogic(await this.reader.TryGetByIdAsync<T>(streamId)),
                result => result != null,
                this.pollingInterval,
                this.timeToWait,
                 this.maxInterval
            );

            this.log.Verbose($"{sagaName} saga awaiting ended successfully");
            return result!;
        }

        public async Task<IHandlingResult> AwaitForever<T>(string streamId, Func<T?, IHandlingResult?> awaitLogic) where T : class, IEventSourced
        {
            var awaitsCount = 0;
            do
            {
                awaitsCount += 1;
                try
                {
                    return await this.Await(streamId, awaitLogic);
                }
                catch (Exception ex)
                {
                    this.log.Error(ex, $"An error ocurred while awaiting saga. The await will start again. Await number: {awaitsCount}.");
                }
            } while (true);
        }

        public async Task<IHandlingResult> AwaitForever<T>(string streamId, Func<T?, Task<IHandlingResult?>> awaitLogic) where T : class, IEventSourced
        {
            var awaitsCount = 0;
            do
            {
                awaitsCount += 1;
                try
                {
                    return await this.Await(streamId, awaitLogic);
                }
                catch (Exception ex)
                {
                    this.log.Error(ex, $"An error ocurred while awaiting saga. The await will start again. Await number: {awaitsCount}.");
                }
            } while (true);
        }
    }
}
