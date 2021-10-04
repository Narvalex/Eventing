using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Utils
{
    public static class TaskRetryFactory
    {
        public static async Task<T> Get<T>(Func<Task<T>> function, Func<Exception, bool> shouldContinue, TimeSpan interval, TimeSpan timeout)
        {
            try
            {
                return await function();
            }
            catch (Exception exception)
            {
                Ensure.NotNegative(interval.TotalMilliseconds, nameof(interval));
                Ensure.NotNegative(timeout.TotalMilliseconds, nameof(timeout));

                var expirationTime = DateTime.UtcNow.Add(timeout);

                var attempts = 1;
                do
                {
                    if (!shouldContinue.Invoke(exception))
                        throw exception;
                    if (DateTime.UtcNow > expirationTime)
                        throw new TimeoutException($"Timeout on retry task factory. Attempts made: {attempts}.", exception);
                    else
                    {
                        await Task.Delay(interval);

                        try
                        {
                            return await function();
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                            attempts++;
                        }
                    }
                        
                } while (true);
            }
        }

        public static async Task StartNew(Func<Task> action, Action<Exception> onException, TimeSpan interval, CancellationToken cancellationToken)
        {
            try
            {
                await action();
            }
            catch (Exception exception)
            {
                onException(exception);

                Ensure.NotNegative(interval.TotalMilliseconds, nameof(interval));

                var attempts = 1;
                do
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException($"Canceled retry. Attempts made: {attempts}.", exception, cancellationToken);
                    else
                    {
                        await Task.Delay(interval, cancellationToken);
                        try
                        {
                            await action();
                            return;
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                            attempts++;
                            onException(ex);
                        }
                    }

                } while (true);
            }
        }

        public static async Task<T> Get<T>(Func<Task<T>> function, Action<Exception> onException, TimeSpan interval, CancellationToken cancellationToken)
        {
            try
            {
                return await function();
            }
            catch (Exception exception)
            {
                onException(exception);

                Ensure.NotNegative(interval.TotalMilliseconds, nameof(interval));

                var attempts = 1;
                do
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException($"Canceled retry. Attempts made: {attempts}.", exception, cancellationToken);
                    else
                    {
                        await Task.Delay(interval, cancellationToken);
                        try
                        {
                            return await function();
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                            attempts++;
                            onException(ex);
                        }
                    }

                } while (true);
            }
        }

        public static async Task<T> StartPolling<T>(Func<T> function, Func<T, bool> valueIsAcceptable, TimeSpan interval, TimeSpan timeout, TimeSpan? maxInterval = null)
        {
            var value = function();
            var acceptable = valueIsAcceptable(value);
            var randomize = maxInterval is not null;
            Random? random = randomize ? new Random() : null;
            int? maxIntervalMilliseconds = randomize ? (int)maxInterval!.Value.TotalMilliseconds : null;

            if (!acceptable)
            {
                Ensure.NotNegative(interval.TotalMilliseconds, nameof(interval));
                Ensure.NotNegative(timeout.TotalMilliseconds, nameof(timeout));

                var expirationTime = DateTime.UtcNow.Add(timeout);
                var attempts = 1;
                do
                {
                    if (DateTime.UtcNow >= expirationTime)
                        throw new TimeoutException($"Timeout on retry task factory. Attempts made: {attempts}.");

                    await Task.Delay(interval);
                    attempts++;
                    value = function();
                    acceptable = valueIsAcceptable(value);
                    if (!acceptable && randomize)
                        interval = TimeSpan.FromMilliseconds(random!.Next(0, maxIntervalMilliseconds!.Value));
                }
                while (!acceptable);
            }

            return value;
        }

        public static async Task<T> StartPollingAsync<T>(Func<Task<T>> function, Func<T, bool> valueIsAcceptable, TimeSpan interval, TimeSpan timeout, TimeSpan? maxInterval = null)
        {
            var value = await function();
            var acceptable = valueIsAcceptable(value);
            var randomize = maxInterval is not null;
            Random? random = randomize ? new Random() : null;
            int? maxIntervalMilliseconds = randomize ? (int)maxInterval!.Value.TotalMilliseconds : null;

            if (!acceptable)
            {
                Ensure.NotNegative(interval.TotalMilliseconds, nameof(interval));
                Ensure.NotNegative(timeout.TotalMilliseconds, nameof(timeout));

                var expirationTime = DateTime.UtcNow.Add(timeout);
                var attempts = 1;
                do
                {
                    if (DateTime.UtcNow >= expirationTime)
                        throw new TimeoutException($"Timeout on retry task factory. Attempts made: {attempts}.");

                    await Task.Delay(interval);
                    attempts++;
                    value = await function();
                    acceptable = valueIsAcceptable(value);
                    if (!acceptable && randomize)
                        interval = TimeSpan.FromMilliseconds(random!.Next(0, maxIntervalMilliseconds!.Value));
                }
                while (!acceptable);
            }

            return value;
        }
    }
}
