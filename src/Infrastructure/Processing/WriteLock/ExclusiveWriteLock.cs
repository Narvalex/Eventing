using Infrastructure.Cryptography;
using Infrastructure.Logging;
using Infrastructure.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Processing
{
    public class ExclusiveWriteLock : IExclusiveWriteLock
    {
        private ILogLite log = LogManager.GetLoggerFor<ExclusiveWriteLock>();
        private readonly string mutexName;
        private Mutex? writeMutex;
        private bool isAcquired = false;
        private TimeSpan mutexPollingInterval = TimeSpan.FromSeconds(1);
        private bool lockAcquisitionStarted = false;
        private object startLockObject = new object();

        public bool IsAcquired => this.isAcquired;

        public ExclusiveWriteLock(IEncryptor encryptor, string key)
        {
            Ensure.NotNull(encryptor, nameof(encryptor));
            Ensure.NotEmpty(key, nameof(key));

            this.mutexName = $"Global:{encryptor.Encrypt(key)}";
        }

        public void StartLockAcquisitionProcess(CancellationToken token)
        {
            lock (this.startLockObject)
            {
                if (this.lockAcquisitionStarted)
                    return;

                this.lockAcquisitionStarted = true;
            }

            Task.Factory.StartNew(() =>
            {
                do
                {
                    try
                    {
                        this.writeMutex?.Dispose();
                        this.writeMutex = new Mutex(true, this.mutexName, out this.isAcquired);
                        while (!this.isAcquired)
                        {
                            this.writeMutex.WaitOne(this.mutexPollingInterval);
                        }
                    }
                    catch (AbandonedMutexException ex)
                    {
                        this.log.Warning($"Write mutex '{this.mutexName}' is said to be abandoned. Probably previous instance of app was terminated abruptly.");
                    }
                    catch (Exception ex)
                    {
                        var secs = 30;
                        this.log.Error(ex, $"Error on acquiring mutex. Acquisition will be retried in {secs} seconds.");
                        Thread.Sleep(TimeSpan.FromSeconds(secs));
                    }

                } while (!this.isAcquired);

            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public async Task WaitLockAcquisition(CancellationToken token)
        {
            while (!this.isAcquired)
            {
                if (!token.IsCancellationRequested)
                    await Task.Delay(this.mutexPollingInterval);
                else
                    throw new TaskCanceledException();
            }
        }

        public async Task WaitLockAcquisition(TimeSpan timeout)
        {
            await TaskRetryFactory.StartPolling(() => this.isAcquired, x => x, this.mutexPollingInterval, timeout);
        }

        public void Dispose()
        {
            using (this.writeMutex)
            {
                try
                {
                    this.writeMutex?.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    this.log.Error(ex, "An error ocurred on mutex disposing");
                }
                finally
                {
                    this.isAcquired = false;
                }
            }
        }
    }
}
