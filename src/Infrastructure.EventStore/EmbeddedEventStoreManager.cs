using Infrastructure.Logging;
using Infrastructure.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Infrastructure.EventStore
{
    public class EmbeddedEventStoreManager : IDisposable
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<EmbeddedEventStoreManager>();

        private readonly string path = @".\EventStore\EventStore.ClusterNode.exe";
        private readonly string args;
        private readonly bool showLog;

        private Process dbProcess;
        private ChildProcessManagement proccessManager;

        public EmbeddedEventStoreManager(
             string ip = "127.0.0.1",
            int intTcpPort = 1112,
            int extTcpPort = 1113,
            int extHttpPort = 2113,
            int intTcpHeartbeatTimeout = 5000,
            int extTcpHeartbeatTimeout = 5000,
            bool showLog = true)
        {
            /*
             WARNING: 
             DONT DELETE THIS INLINE CONFIG. The reason to not use an eventstore config file is to 
             keep in a single place the CONNECTION and the CREATION of the db instance in a single place
             */
            
            Ensure.NotEmpty(ip, nameof(ip));
            Ensure.Positive(intTcpHeartbeatTimeout, nameof(intTcpHeartbeatTimeout));
            Ensure.Positive(extTcpHeartbeatTimeout, nameof(extTcpHeartbeatTimeout));

            this.args = new StringBuilder()
            // This option killed projections when migrating from 5.0.0 to 5.0.1
            .Append($" --int-tcp-heartbeat-timeout={intTcpHeartbeatTimeout}")
            .Append($" --ext-tcp-heartbeat-timeout={extTcpHeartbeatTimeout}")
            .Append($" --int-ip={ip} --ext-ip={ip} ")
            .Append($" --int-tcp-port={intTcpPort} --ext-tcp-port={extTcpPort}")
            .Append($" --http-port={extHttpPort}")
            // Stats are now disabled by default
            //.Append($" --stats-period-sec={statsPeriodSec}")
            .Append($" --skip-db-verify=true")
            .Append($" --skip-index-verify=true")
            .Append($" --optimize-index-merge=true")
            // There is no more docs for the following param
            // See: https://www.eventstore.com/blog/event-store-5.0.0-release
            // And: https://github.com/EventStore/EventStore/pull/1639
            .Append($" --reduce-file-cache-pressure=true")

            // Added in version 20.x
            // # Run in insecure mode
            .Append($" --insecure=true")
            // # Network configuration
            .Append($" --enable-external-tcp=true")
            .Append($" --enable-atom-pub-over-http=true")
            // # Projections configuration
            //.Append($"--start-standard-projections=true")
            //.Append($" --run-projections=all") //No more projections in Erp.V3
            .Append($" --run-projections=none")

            .ToString();

            this.showLog = showLog;
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
                return;

            this.log.Info("Starting a new instance of EventStore...");
            this.dbProcess = new Process()
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    //RedirectStandardOutput = !this.showLog,
                    CreateNoWindow = !showLog,
                    FileName = this.path,
                    Arguments = this.args,
                    Verb = "runas"
                }
            };

            this.dbProcess.Start();
            this.proccessManager = new ChildProcessManagement();
            this.proccessManager.AddProcess(this.dbProcess);

            cancellationToken.Register(() => this.Stop());

            this.log.Success("Event Store is now running");
        }

        public void Stop()
        {
            using (this.proccessManager)
            using (this.dbProcess)
            {
                if (this.dbProcess != null && !this.dbProcess.HasExited)
                {
                    // These may be called concurrently....
                    this.dbProcess?.Kill();
                    this.dbProcess?.WaitForExit();
                }
            }
            this.dbProcess = null;
            this.proccessManager = null;
        }

        public void Dispose()
        {
            // this method will dispose everything, anyway
            this.Stop();
        }

        public void DropDatabase()
        {
            try
            {
                var dbPath = @".\EventStore\data";
                this.log.Info("Dropping EventStore. Checking if EventStore database exits...");
                if (Directory.Exists(dbPath))
                {
                    this.log.Info("EventStore database was found. Deleting now...");

                    // You will need User Elevated Rights
                    // https://stackoverflow.com/questions/2282448/windows-7-and-vista-uac-programmatically-requesting-elevation-in-c-sharp
                    // You might check this out:
                    // https://stackoverflow.com/questions/1157246/unauthorizedaccessexception-trying-to-delete-a-file-in-a-folder-where-i-can-dele

                    Directory.Delete(dbPath, true);
                    this.log.Info("EventStore database deleted");
                }
                else
                    this.log.Warning("The dabase was not found! No database where deleted!");

                var logPath = @".\EventStore\logs";
                if (Directory.Exists(logPath))
                {
                    this.log.Info("Old db logs where found. Deleting now...");
                    Directory.Delete(logPath, true);
                    this.log.Info("EventStore old logs were deleted");
                }
                else
                    this.log.Warning("The old db logs were not found!");
            }
            catch (Exception ex)
            {
                this.log.Error(ex, "Error on droping EventStoreDb");
                throw;
            }
          
        }
    }
}
