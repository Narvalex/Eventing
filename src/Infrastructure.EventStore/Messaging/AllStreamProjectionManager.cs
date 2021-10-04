using Infrastructure.Logging;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EventStore.Messaging
{
    public class AllStreamProjectionManager
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<AllStreamProjectionManager>();

        private readonly IEsProjectionClient client;
        private readonly string[] projections = new string[] {
            "$by_category",
            "$by_correlation_id",
            "$by_event_type",
            "$stream_by_category",
            "$streams",
            AllStreamProjection.EmittedStreamName
        };

        public AllStreamProjectionManager(IEsProjectionClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Created the projection if not found.
        /// </summary>
        /// <returns>False if not found. True if it is found.</returns>
        public async Task<bool> EnsureIsCreatedAndIsRunning()
        {
            var projectionName = AllStreamProjection.EmittedStreamName;

            this.log.Info($"Checking now projection '{projectionName}' from EventStore...");
            var script = await this.client.GetScriptAsync(projectionName);

            var created = script != null;
            if (!created)
            {
                this.log.Info($"The projection '{projectionName}' was not found. Creating now...");
                await this.client.CreateContinuousAsync(projectionName, AllStreamProjection.Script);
            }
            else 
            {
                if (script.Trim() != AllStreamProjection.Script.Trim())
                    throw new InvalidOperationException($"The script for projection {projectionName} is different from expected.");
            }

            this.log.Info($"The projection '{projectionName}' looks good. Ensuring that all projections are running...");

            for (int i = 0; i < this.projections.Length; i++)
            {
                await this.client.EnableAsync(this.projections[i]);
                this.log.Info($"Projection {this.projections[i]} is enabled");
            }

            this.log.Info("All projections are running");
            return created;
        }
    }
}
