using Infrastructure.Processing;
using System;
using System.Threading;

namespace Infrastructure.RelationalDbSync
{
    public interface ITableSynchronizer
    {
        void Start(CancellationToken token, IDynamicThrottling dynamicThrottling, TimeSpan idleInterval, TimeSpan activeTableInterval, TimeSpan pageIntervalSec, TimeSpan onErrorInterval, int pageSize = 500);
        void Stop();
        string TableName { get; }
    }
}
