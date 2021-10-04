using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    /// <summary>
    /// This is usefull to abstract out the task of creating 
    /// a new subscription in a component that does not know 
    /// what is the underlying transport mechanism.
    /// </summary>
    public interface IEventSubscriptionFactory
    {
        Task<IEventSubscription> Create();
    }
}