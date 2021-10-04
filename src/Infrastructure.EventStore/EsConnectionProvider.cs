using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Infrastructure.Logging;
using Infrastructure.Utils;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.EventStore
{
    public class EsConnectionProvider : IDisposable
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<EsConnectionProvider>();
        private bool disposed = false;

        private readonly IPEndPoint endPoint;
        private readonly UserCredentials credentials;

        private readonly string resilientConnNamePrefix;
        private readonly string failFastConnNamePrefix;

        private bool failFastConnEstablished = false;
        private bool resilientConnEstablished = false;

        private IEventStoreConnection failFastConnection;
        private IEventStoreConnection resilientConnection;

        private int failFastConnNumber = 0;

        private readonly SemaphoreSlim failFastSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim resilientSemaphore = new SemaphoreSlim(1, 1);
        private readonly int tcpHeartbeatTimeout;
        private readonly bool showLog;

        public EsConnectionProvider(
            string ip = "127.0.0.1",
            int extTcpPort = 1113,
            string username = "admin",
            string password = "changeIt",
            string resilientConnNamePrefix = "default-resilient",
            string failFastConnNamePrefix = "default-failfast",
            int tcpHeartbeatTimeout = 500,
            bool showLog = false)
        {
            Ensure.NotEmpty(ip, nameof(ip));

            this.endPoint = new IPEndPoint(ip == "127.0.0.1" ? IPAddress.Loopback : IPAddress.Parse(ip), extTcpPort);
            this.credentials = new UserCredentials(username, password);

            this.resilientConnNamePrefix = Ensured.NotEmpty(resilientConnNamePrefix, nameof(resilientConnNamePrefix));
            this.failFastConnNamePrefix = Ensured.NotEmpty(failFastConnNamePrefix, nameof(failFastConnNamePrefix));
            this.tcpHeartbeatTimeout = Ensured.Positive(tcpHeartbeatTimeout, nameof(tcpHeartbeatTimeout));
            this.showLog = showLog;
        }

        public async Task<IEventStoreConnection> GetFailFastConnection()
        {
            if (!this.failFastConnEstablished)
            {
                // This code is only for initialization. The connection will always be restablished automatically.
                await this.EstablishFailFastConnectionAsync();
            }

            return this.failFastConnection;
        }

        public async Task<IEventStoreConnection> GetResilientConnection()
        {
            if (!this.resilientConnEstablished)
            {
                // This code is only for initialization. The connection will always be restablished automatically.
                await this.EstablishResilientConnectionAsync();
            }

            return this.resilientConnection;
        }

        private async Task EstablishFailFastConnectionAsync()
        {
            await this.failFastSemaphore.WaitAsync();
            try
            {
                if (this.failFastConnEstablished)
                    return;

                var settings = ConnectionSettings
                                .Create()
                                .Tap(x => 
                                {
                                    if (this.showLog)
                                        x.UseConsoleLogger();
                                })
                                .SetDefaultUserCredentials(this.credentials)
                                .SetHeartbeatTimeout(TimeSpan.FromMilliseconds(this.tcpHeartbeatTimeout))
                                .DisableTls()
                                .DisableServerCertificateValidation()
                                .Build();

                var connName = $"{this.failFastConnNamePrefix}-{Interlocked.Increment(ref failFastConnNumber)}-{DateTime.UtcNow.ToString("dd/MM/yyyy-hh:mm:ss")}";

                this.failFastConnection =
                    EventStoreConnection.Create(
                       settings,
                       this.endPoint,
                       connName);

                this.failFastConnection.Closed += async (s, e) => 
                    await this.EstablishFailFastConnectionAsync();

                await this.failFastConnection.ConnectAsync();

                this.failFastConnEstablished = await this.TryWaitConnection(this.failFastConnection);
                if (this.failFastConnEstablished)
                    this.log.Info("Fail fast connection established");

                
            }
            catch { throw; }
            finally { this.failFastSemaphore.Release(); }
        }

        private async Task EstablishResilientConnectionAsync()
        {
            await this.resilientSemaphore.WaitAsync();
            try
            {
                if (this.resilientConnEstablished)
                    return;

                var settings = ConnectionSettings
                                .Create()
                                .Tap(x =>
                                {
                                    if (this.showLog)
                                        x.UseConsoleLogger();
                                })
                                .SetHeartbeatTimeout(TimeSpan.FromMilliseconds(this.tcpHeartbeatTimeout))
                                .KeepReconnecting()
                                .KeepRetrying()
                                .SetDefaultUserCredentials(this.credentials)
                                .DisableTls()
                                .DisableServerCertificateValidation()
                                .Build();

                var connName = $"{this.resilientConnNamePrefix}-{DateTime.UtcNow.ToString("dd/MM/yyyy-hh:mm:ss")}";

                this.resilientConnection = EventStoreConnection.Create(
                                            settings,
                                            this.endPoint,
                                            connName);

                await this.resilientConnection.ConnectAsync();

                if (await this.TryWaitConnection(this.resilientConnection))
                    this.log.Info("Resilient connection established");

                this.resilientConnEstablished = true;
            }
            catch { throw; }
            finally { this.resilientSemaphore.Release(); }
        }

        // Not authenticated exception thows when not waiting for this after db deleted? Why?
        private async Task<bool> TryWaitConnection(IEventStoreConnection connection)
        {
            try
            {
                await TaskRetryFactory.Get(
                                       async () => await connection.ReadEventAsync("foo", 0, false),
                                       ex =>
                                       {
                                           this.log.Verbose(ex.Message);
                                           return !this.disposed;
                                       },
                                       TimeSpan.FromMilliseconds(100),
                                       TimeSpan.FromSeconds(30));

                return true;
            }
            catch (Exception ex)
            {
                this.log.Error(ex, $"Await for connection failed.");
                return false;
            }
        }

        public void Dispose()
        {
            using (this.resilientConnection)
            using (this.failFastConnection)
            {
                this.resilientConnection?.Close();
                this.failFastConnection?.Close();
            }

            this.disposed = true;
        }
    }
}
