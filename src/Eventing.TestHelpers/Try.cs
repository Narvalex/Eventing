using System;
using System.Diagnostics;
using System.Threading;

namespace Eventing.TestHelpers
{
    public static class Is
    {
        public static void TrueThat(Func<bool> predicate, TimeSpan timeout)
        {
            Ensure.Positive(timeout.TotalMilliseconds, nameof(timeout));

            var sleepMilliseconds = 1;
            var sw = new Stopwatch();
            sw.Start();
            while (!predicate.Invoke())
            {
                if (sw.Elapsed >= timeout)
                    throw new TimeoutException("The predicate has timeout");
                Thread.Sleep(sleepMilliseconds);
                sleepMilliseconds = sleepMilliseconds * 2;
            }
        }
    }
}
