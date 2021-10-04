using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using Infrastructure.Logging;
using Infrastructure.Utils;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.EventStore.Messaging
{
    public class ProjectionDefinition
    {
        private readonly ILogLite logger = LogManager.GetLoggerFor<ProjectionDefinition>();

        private ProjectionsManager manager;
        private UserCredentials credentials;
        private string projectionName;
        private string projectionScript;

        private bool retrying = false;
        private bool isUpToDateAndRunning = false;

        internal ProjectionDefinition(ProjectionsManager manager, UserCredentials credentials, string projectionName, string emittedStream, ICollection<string> streams)
        {
            Ensure.NotNull(manager, nameof(manager));
            Ensure.NotNull(credentials, nameof(credentials));
            Ensure.NotEmpty(projectionName, nameof(projectionName));
            Ensure.NotEmpty(emittedStream, nameof(emittedStream));
            Ensure.NotNull(streams, nameof(streams));
            Ensure.Positive(streams.Count, nameof(streams));

            this.manager = manager;
            this.credentials = credentials;
            this.projectionName = projectionName;
            this.EmittedStream = emittedStream;
            this.projectionScript = buildScript(emittedStream, streams);
        }

        public static ProjectionDefinitionInitBuilder New(string projectionName, string emittedStream, ProjectionsManager manager, UserCredentials credentials)
            => new ProjectionDefinitionInitBuilder(projectionName, emittedStream, manager, credentials);

        public string EmittedStream { get; }

        public async Task EnsureThatIsUpToDateAndRunning()
        {
            if (this.isUpToDateAndRunning) return;

            if (retrying)
                this.logger.Verbose($"Retrying on checking existence of projection {projectionName}...");
            else
                this.logger.Verbose($"Ensuring existence of projection {projectionName}...");

            string persistedScript;
            bool existeLaProyeccion;
            try
            {
                persistedScript = await this.manager.GetQueryAsync(this.projectionName, this.credentials);
                existeLaProyeccion = true;
            }
            catch (ProjectionCommandFailedException ex)
            {
                if (ex.HttpStatusCode == 404)
                {
                    existeLaProyeccion = false;
                    persistedScript = null;
                }
                else
                {
                    this.retrying = true;
                    throw;
                }
            }


            if (existeLaProyeccion && this.projectionScript != persistedScript)
            {
                this.logger.Info($"The projection {this.projectionName} was found but a new version is available. Updating now...");
                await this.manager.UpdateQueryAsync(this.projectionName, this.projectionScript, credentials);
                this.logger.Success($"The projection {this.projectionName} was successfully updated!");
            }
            else if (!existeLaProyeccion)
            {
                this.logger.Verbose($"The projection {this.projectionName} was NOT FOUND. Creating projection...");
                await this.manager.CreateContinuousAsync(this.projectionName, this.projectionScript, credentials);
            }
            else
                this.logger.Verbose($"The projection {this.projectionName} was found and is up to date!");

            await this.manager.EnableAsync(this.projectionName, credentials);

            this.isUpToDateAndRunning = true;

            this.logger.Verbose($"The projection {this.projectionName} is up and running!");
        }

        private static string buildScript(string emittedStream, ICollection<string> streams)
        {
            var sb = new StringBuilder();
            streams.ForEach(s => sb.AppendLine($"            case '{s}':"));
            var streamsToProject = sb.ToString().TrimEnd();

            var script = $@"
fromAll()
.when({{
    '$any': (s, e) => {{
        let streamId = e.streamId;
        if (streamId === undefined || streamId === null) return;
        let category = streamId.split('-')[0];
        
        switch(category) {{
{streamsToProject}
                linkTo('{emittedStream}', e);
                break;
            default:
                return;
        }}
    }}
}});
";

            return script.TrimStart();
        }
    }
}
