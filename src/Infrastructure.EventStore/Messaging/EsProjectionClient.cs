using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using Infrastructure.Utils;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Infrastructure.EventStore.Messaging
{
    public class EsProjectionClient : IEsProjectionClient
    {
        // This mantains a connection to EventStore... not good!
        private readonly UserCredentials credentials;

        private Func<ProjectionsManager> managerFactory;

        public EsProjectionClient(string ip, int externalHttpPort, TimeSpan operationTimeout, UserCredentials credentials)
        {
            this.managerFactory = () => new ProjectionsManager(
                new global::EventStore.ClientAPI.Common.Log.ConsoleLogger(),
                new IPEndPoint(IPAddress.Parse(ip), externalHttpPort),
                operationTimeout);

            //this.manager = this.managerFactory.Invoke();

            this.credentials = Ensured.NotNull(credentials, nameof(credentials));
        }

        public async Task CreateContinuousAsync(string projectionName, string script)
        {
            await this.managerFactory().CreateContinuousAsync(projectionName, script, this.credentials);
            await this.managerFactory().EnableAsync(projectionName, this.credentials);
        }

        public async Task<string> GetScriptAsync(string projectionName)
        {
            try
            {
                var script = await this.managerFactory().GetQueryAsync(projectionName, this.credentials);
                return script;
            }
            catch (ProjectionCommandFailedException ex)
            {
                if (ex.HttpStatusCode == 404)
                    return null;
                else
                    throw ex;
            }
        }

        public async Task UpdateAsync(string projectionName, string script)
        {
            await this.managerFactory().UpdateQueryAsync(projectionName, script, this.credentials);
            await this.managerFactory().EnableAsync(projectionName, this.credentials);
        }

        public async Task EnableAsync(string projectionName)
        {
            await this.managerFactory.Invoke().EnableAsync(projectionName, this.credentials);
        }
    }
}
