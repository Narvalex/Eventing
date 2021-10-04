using Infrastructure.DateTimeProvider;
using Infrastructure.Logging;
using Infrastructure.Utils;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Infrastructure.Snapshotting
{
    internal class SnapshotEvictionLogger
    {
        private readonly ILogLite log;
        private readonly IDateTimeProvider dateTimeProvider = new LocalDateTimeProvider();
        private readonly TimeSpan intervalToLog = TimeSpan.FromHours(1);
        private DateTime startTime;
        private bool started = false;
        private int count = 0;
        private object lockObject = new object();

        public SnapshotEvictionLogger(ILogLite log)
        {
            this.log = log.EnsuredNotNull(nameof(log));
        }

        internal void CacheEvictionCallback(object key, object _, EvictionReason reason, object __)
        {
            lock (this.lockObject)
            {
                if (!this.started)
                {
                    LogNow(log, key, reason, 1);
                    this.started = true;
                    this.startTime = this.dateTimeProvider.Now;
                }
                else
                {
                    this.count += 1;
                    var now = this.dateTimeProvider.Now;
                    if ((now - this.startTime) >= this.intervalToLog)
                    {
                        LogNow(log, key, reason, this.count);
                        this.startTime = this.dateTimeProvider.Now;
                        this.count = 0;
                    }
                }
            }
        }

        private static void LogNow(ILogLite log, object key, EvictionReason reason, int count)
        {
            string message;
            switch (reason)
            {
                case EvictionReason.None:
                    message = $"The snapshot with key {key} was evicted. Reason: none";
                    break;
                case EvictionReason.Removed:
                    message = $"The snapshot with key {key} was evicted. Reason: removed";
                    break;
                case EvictionReason.Expired:
                    message = $"The snapshot with key {key} was evicted. Reason: expired";
                    break;
                case EvictionReason.TokenExpired:
                    message = $"The snapshot with key {key} was evicted. Reason: token expired";
                    break;
                case EvictionReason.Capacity:
                    message = $"The snapshot with key {key} was evicted. Reason: capacity";
                    break;

                case EvictionReason.Replaced:
                default:
                    return;
            }

            log.Info($"{message}. Total evicted since last notification: {count}.");
        }
    }
}
