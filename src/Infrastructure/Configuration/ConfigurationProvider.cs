using Infrastructure.Logging;
using Infrastructure.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;

namespace Infrastructure.Configuration
{
    // More info: https://stackoverflow.com/questions/38114761/asp-net-core-configuration-for-net-core-console-application
    // To detect Visual Studio file changes: https://github.com/dotnet/corefx/issues/9462
    public abstract class ConfigurationProvider<T> : IConfigurationProvider<T>, IDisposable where T : class
    {
        private readonly string jsonFile;
        private Lazy<T> lazyConfig;
        private T configuration = null;
        private readonly object lockObject = new object();
        private readonly FileSystemWatcher watcher;
        protected ILogLite log = LogManager.GetLoggerFor<ConfigurationProvider>();
        private TimeSpan waitInterval = TimeSpan.FromSeconds(1);
        private Timer timer = null;

        public ConfigurationProvider(string jsonFile)
        {
            this.jsonFile = Ensured.NotNull(jsonFile, nameof(jsonFile));
            this.watcher = new FileSystemWatcher();
            this.lazyConfig = new Lazy<T>(this.BuildConfigurationFirstTime);
        }

        public T Configuration => this.configuration ?? this.lazyConfig.Value;

        public event EventHandler<ConfigurationChanged> ConfigurationChanged;

        protected virtual void OnConfigParsing(T config, IConfigurationRoot configRoot)
        {
        }

        public void Dispose()
        {
            lock (this.lockObject)
            {
                if (this.configuration != null)
                    this.watcher.Changed -= this.OnFileChanged;

                this.configuration = null;
            }

            using (this.timer)
            using (this.watcher)
            {
            }
        }

        private T BuildConfigurationFirstTime()
        {
            lock (this.lockObject)
            {
                if (this.configuration is null)
                {
                    this.SetConfigFromFile();
                    this.StartFileWatching();
                }

                return this.configuration;
            }
        }

        private void SetConfigFromFile()
        {
            var configRoot = new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile(this.jsonFile)
                                    .Build();

            var config = configRoot.Get<T>();

            this.OnConfigParsing(config, configRoot);

            this.configuration = config;
        }

        private void StartFileWatching()
        {
            this.watcher.Path = Directory.GetCurrentDirectory();
            this.watcher.Filter = this.jsonFile;
            // this prevents to call for multiple changes
            this.watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            this.watcher.Changed += OnFileChanged;
            this.watcher.Renamed += OnFileChanged;
            this.watcher.EnableRaisingEvents = true;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            this.log.Info($"Config file {this.jsonFile} was {e.ChangeType.ToString().ToLower()}!");
            lock (this.lockObject)
            {
                if (this.timer != null)
                    this.timer.Change(this.waitInterval, this.waitInterval);
                else
                {
                    this.timer = new Timer(_ =>
                    {
                        lock (this.lockObject)
                        {
                            using (this.timer)
                            {
                                if (this.timer != null)
                                {
                                    this.SetConfigFromFile();
                                    this.ConfigurationChanged?.Invoke(this, new ConfigurationChanged());
                                    this.timer.Change(Timeout.Infinite, Timeout.Infinite);
                                    this.timer = null;
                                }
                            } 
                        }
                    });

                    this.timer.Change(this.waitInterval, this.waitInterval);
                }
            }
        }
    }
}
