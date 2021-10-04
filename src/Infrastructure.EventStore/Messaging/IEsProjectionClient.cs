using System.Threading.Tasks;

namespace Infrastructure.EventStore.Messaging
{
    public interface IEsProjectionClient
    {
        /// <summary>
        /// Gets the projection from the EventStore database.
        /// </summary>
        /// <returns>The projection script or null if not found</returns>
        Task<string> GetScriptAsync(string projectionName);

        /// <summary>
        /// Creates a continous projection and starts.
        /// </summary>
        Task CreateContinuousAsync(string projectionName, string script);

        /// <summary>
        /// Updates a continous projection and makes sure is enabled.
        /// </summary>
        Task UpdateAsync(string projectionName, string script);

        /// <summary>
        /// Enables a projection
        /// </summary>
        Task EnableAsync(string projectionName);
    }
}
