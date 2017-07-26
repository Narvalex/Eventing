using Eventing.GetEventStore;
using Inventory.Common;

namespace Inventory.Server
{
    public static class ServiceLocator
    {
        private static SimpleContainer _container = new SimpleContainer();

        public static T ResolveSingleton<T>() => _container.ResolveSingleton<T>();

        public static T ResolveNewOf<T>() => _container.ResolveNewOf<T>();

        public static void Initialize()
        {
            var container = _container;

            var esm = new EventStoreManager();

            container.Register<EventStoreManager>(esm);
        }
    }
}
