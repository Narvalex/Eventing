﻿using Eventing.Log;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Eventing.GetEventStore
{
    public class EventStoreManager
    {
        private ILogLite log = LogManager.GetLoggerFor<EventStoreManager>();

        private Process process;
        private readonly string path = @".\EventStore\EventStore.ClusterNode.exe";
        private readonly string args = "--db=./ESData --start-standard-projections=true --run-projections=all --stats-period-sec=86400";

        private readonly UserCredentials userCredentials;
        private readonly IPEndPoint ipEndPoint;

        private readonly string resilientConnectionNamePrefix;
        private readonly string failFastConnectionNamePrefix;

        private static int failFastNumber = 0;

        private bool failFastConnectionWasEstablished = false;
        private bool resilientConnectionWasEstablished = false;

        private IEventStoreConnection failFastConnection = null;
        private IEventStoreConnection resilientConnection = null;

        private readonly object resilientConnectionLock = new object();

        public EventStoreManager(
            string defaultUserName = "admin",
            string defaultPassword = "changeit",
            string extIp = "127.0.0.1",
            int tcpPort = 1113,
            string resilientConnectionNamePrefix = "anonymous-resilient",
            string failFastConnectionNamePrefix = "anonymous-fail-fast")
        {
            Ensure.NotNullOrWhiteSpace(defaultUserName, nameof(defaultUserName));
            Ensure.NotNullOrWhiteSpace(defaultPassword, nameof(defaultPassword));
            Ensure.NotNullOrWhiteSpace(extIp, nameof(extIp));
            Ensure.NotNullOrWhiteSpace(failFastConnectionNamePrefix, nameof(failFastConnectionNamePrefix));
            Ensure.NotNull(resilientConnectionNamePrefix, nameof(resilientConnectionNamePrefix));

            this.args += $" --ext-ip={extIp}";

            this.userCredentials = new UserCredentials(defaultUserName, defaultPassword);
            this.ipEndPoint = new IPEndPoint(IPAddress.Parse(extIp), tcpPort);

            this.resilientConnectionNamePrefix = resilientConnectionNamePrefix;
            this.failFastConnectionNamePrefix = failFastConnectionNamePrefix;
        }

        public void CreateDbIfNotExists() => this.InitializeDb();

        public void DropAndCreateDb()
        {
            var path = @".\ESData";
            this.log.Info("Checking if EventStore database exits...");
            if (Directory.Exists(path))
            {
                this.log.Info("EventStore database was found. Deleting now...");
                Directory.Delete(path, true);
                this.log.Info("EventStore database deleted");
            }
            this.log.Info("Starting EventStore from scratch...");
            this.InitializeDb();
            this.log.Info("EventStore is warming up. Checking a test connections for 3 seconds...");
            var connection = this.GetFailFastConnection().Result;
            connection = this.resilientConnection;
            Thread.Sleep(3000);
            this.log.Info("Restarting the event store now...");
            this.KillEventStoreProcess();
            this.InitializeDb();
            this.log.Info("EventStore connection succeded! EventStore is up and running");
        }

        private void InitializeDb()
        {
            this.process = new Process()
            {
                StartInfo =
                {
                    UseShellExecute = true, // Start in a new console shell
                    CreateNoWindow = false,
                    FileName = this.path,
                    Arguments = this.args,
                    Verb = "runas",
                }
            };

            this.process.Start();
        }

        public async Task<IEventStoreConnection> GetFailFastConnection()
        {
            // This code is only for initialization. The connection will always be restablished automatically.
            if (!this.failFastConnectionWasEstablished)
            {
                await this.EstablishFailFastConnectionAsync();
                this.failFastConnectionWasEstablished = true;
            }

            return this.failFastConnection;
        }

        public IEventStoreConnection ResilientConnection
        {
            get
            {
                // This code is only for initialization. The connection will always be restablished automatically.
                if (!this.resilientConnectionWasEstablished)
                {
                    lock (this.resilientConnectionLock)
                    {
                        if (!this.resilientConnectionWasEstablished)
                        {
                            this.EstablishResilientConnection();
                            this.resilientConnectionWasEstablished = true;
                        }
                    }
                }

                return this.resilientConnection;
            }
        }

        private async Task EstablishFailFastConnectionAsync()
        {
            var settings =
                ConnectionSettings
                .Create()
                //.UseConsoleLogger()
                .SetDefaultUserCredentials(this.userCredentials)
                .Build();

            this.failFastConnection = EventStoreConnection.Create(settings, this.ipEndPoint, this.FormatConnectionName(this.failFastConnectionNamePrefix));
            this.failFastConnection.Closed += async (s, e) => await this.EstablishFailFastConnectionAsync();

            await this.failFastConnection.ConnectAsync();
        }

        private string FormatConnectionName(string prefix)
        {
            return $"{prefix}-{Interlocked.Increment(ref failFastNumber)}-{DateTime.UtcNow.ToString("dd/MM/yyy-hh:mm:ss")}";
        }

        private void EstablishResilientConnection()
        {
            var settings =
                ConnectionSettings
                .Create()
                .KeepReconnecting()
                .KeepRetrying()
                //.UseConsoleLogger()
                .SetDefaultUserCredentials(this.userCredentials)
                .Build();

            this.resilientConnection = EventStoreConnection.Create(settings, this.ipEndPoint, this.FormatConnectionName(this.resilientConnectionNamePrefix));
            this.resilientConnection.ConnectAsync().Wait();
        }

        public void TearDown(bool dropDb = false)
        {
            if (this.failFastConnectionWasEstablished)
                this.failFastConnection.Close();

            if (this.resilientConnectionWasEstablished)
                this.resilientConnection.Close();

            this.KillEventStoreProcess();

            var path = @".\ESData";
            if (dropDb && Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private void KillEventStoreProcess()
        {
            if (this.process != null && !this.process.HasExited)
            {
                this.process.Kill();
                this.process.WaitForExit();
            }
        }
    }
}
