using Infrastructure.Configuration;
using Infrastructure.Logging;
using Infrastructure.Utils;
using System;
using System.Threading;

namespace Infrastructure.Processing
{
    public abstract class MainProcessor<T> : IDisposable
    {
        protected readonly ILogLite log;
        private AutoResetEvent waitHandle = new AutoResetEvent(false);
        protected IConfigurationProvider<T> configProvider;
        protected bool shouldBeRunning;
        protected readonly string processorName;
        protected readonly string[] args;

        public MainProcessor(string processorName, IConfigurationProvider<T> configProvider, string[] args)
        {
            this.processorName = Ensured.NotEmpty(processorName, nameof(processorName));
            this.configProvider = Ensured.NotNull(configProvider, nameof(configProvider));

            this.log = LogManager.GetLoggerFor(this.processorName);
            this.args = args;

            this.configProvider.ConfigurationChanged += (s, e) =>
            {
                //this.log.Warning("App config was modified. The process will recycle now.");
                //this.waitHandle.Set();

                this.log.Warning("App config was modified. The changes will be applied after a restart.");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                //this.log.Warning("The app crashed. The process will try to recycle in 10 seconds.");
                //Thread.Sleep(TimeSpan.FromSeconds(10));
                //this.waitHandle.Set();

                var message = $"The app chashed. Is terminating: {e.IsTerminating}. Error object is: {e.ExceptionObject}";
                this.log.Fatal(message);
                this.shouldBeRunning = false;
                this.waitHandle.Set();
                throw new Exception(message);
            };
        }

        public void Run()
        {
            this.shouldBeRunning = true;
            do
                using (var cancellation = new ProcessCancellationAwaiter(this.waitHandle))
                {
                    this.KeepRunning(this.configProvider.Configuration, cancellation);
                }
            while (this.shouldBeRunning);
                
        }

        protected abstract void KeepRunning(T config, ProcessCancellationAwaiter processCancellation);

        ~MainProcessor()
        {
            this.Dispose();
        }


        // Check this: https://stackoverflow.com/questions/18336856/implementing-idisposable-correctly
        public void Dispose()
        {
            this.shouldBeRunning = false;
            using (this.waitHandle)
            {
                this.waitHandle.Set();
                this.OnDisposing();
            }
        }

        protected virtual void OnDisposing()
        { }
    }
}
