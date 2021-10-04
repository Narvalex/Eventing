using Infrastructure.Logging;
using Infrastructure.Utils;
using System.Threading.Tasks;

namespace Infrastructure.EventStore.Messaging
{
    public class ProjectionVersionManager
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<ProjectionVersionManager>();
        private readonly IEsProjectionClient client;

        public ProjectionVersionManager(IEsProjectionClient client)
        {
            this.client = Ensured.NotNull(client, nameof(client));
        }

        /// <summary>
        /// Ensures the app event stream projection is updated
        /// </summary>
        /// <typeparam name="T">The static class that contains all categories</typeparam>
        /// <param name="projectionName">The projection output and the projection name</param>
        /// <returns>True if the projection found in database was found already updated, otherwise false.</returns>
        public async Task EnsureProjectionExistsAndIsUptoDate<T>(string projectionName)
        {
            var streams = Ensured.AllUniqueConstStrings<T>();
            var appScript = StreamJoinProjectionScript.Generate(projectionName, streams);

            this.log.Info($"Checking now projection '{projectionName}' from EventStore");
            var dbScript = await this.client.GetScriptAsync(projectionName);


            if (!this.DatabaseScriptIsStale(appScript, dbScript))
            {
                this.log.Info($"The projection '{projectionName}' is up to date.");
                return;
            }
                

            if (dbScript is null)
            {
                await this.client.CreateContinuousAsync(projectionName, appScript);
                this.log.Info($"The projection '{projectionName}' was created.");
            }
            else
            {
                await this.client.UpdateAsync(projectionName, appScript);
                this.log.Info($"The projection '{projectionName}' was updated.");
            }
        }

        /// <summary>
        /// This logic makes the hard decision to update or not update.
        /// </summary>
        private bool DatabaseScriptIsStale(string appScript, string dbScript)
        {
            if (dbScript is null)
                return true;

            var db = dbScript.Trim();
            var app = appScript.Trim();

            if (app == db)
                return false;

            return app.Length >= db.Length;
        }
    }
}
