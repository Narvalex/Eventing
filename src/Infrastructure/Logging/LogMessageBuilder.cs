using Infrastructure.Utils;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Infrastructure.Logging
{
    /// <summary>
    /// A log message builder
    /// </summary>
    public class LogMessageBuilder
    {
        private static readonly int _processId = Process.GetCurrentProcess().Id;
        private readonly string componentName;
        private readonly bool verbose;

        public LogMessageBuilder(string componentName, bool verbose)
        {
            Ensure.NotEmpty(componentName, nameof(componentName));

            this.componentName = componentName;
            this.verbose = verbose;
        }

        public string BuildMessage(string level, string format, params object[] args)
        {
            if (this.verbose)
                return string.Format("[{0:00000},{1:00} {2:dd/MM/yy HH:mm:ss.fff} {3} {4}] {5}",
                                        _processId,
                                        Thread.CurrentThread.ManagedThreadId,
                                        DateTime.Now,
                                        this.componentName,
                                        level,
                                        args.Length == 0 ? format : string.Format(format, args));
            else
                return string.Format("[{0:dd/MM/yy HH:mm:ss.fff} {1} {2}] {3}",
                                       DateTime.Now,
                                       this.componentName,
                                       level,
                                       args.Length == 0 ? format : string.Format(format, args));
        }

        public string BuildMessage(Exception ex, string level, string format, params object[] args)
        {
            var stringBuilder = new StringBuilder();
            while (ex != null)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(ex.ToString());
                ex = ex.InnerException;
            }

            return string.Format("[{0:dd/MM/yy HH:mm:ss.fff} {1} {2}] {3}\nEXCEPTION(S) OCCURRED:{4}",
                                 DateTime.Now,
                                 this.componentName,
                                 level,
                                 args.Length == 0 ? format : string.Format(format, args),
                                 stringBuilder);
        }
    }
}
