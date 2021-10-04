using Infrastructure.Messaging.Handling;

namespace Infrastructure.EntityFramework.Messaging.Handling
{
    public interface IReadModelCheckpointStoreRegistry
    {
        void Register(IEfDbInitializer readModelDbInitializer);
    }
}
