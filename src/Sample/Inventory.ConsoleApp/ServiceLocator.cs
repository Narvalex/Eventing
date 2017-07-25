using Eventing.Client.Http;
using Inventory.Client;
using Inventory.Common;

namespace Inventory.ConsoleApp
{
    public static class ServiceLocator
    {
        private static SimpleContainer _container = new SimpleContainer();

        public static T ResolveSingleton<T>() => _container.ResolveSingleton<T>();

        public static T ResolveNewOf<T>() => _container.ResolveNewOf<T>();

        public static void Initialize()
        {
            var container = _container;

            var httpLite = new HttpLite(AppConfig.ServerBaseUrl);

            var inventoryClient = new InventoryClient(httpLite);

            var inventoryController = new InventoryController(new InventoryView(), inventoryClient);

            var mainView = new MainView();
            var mainController = new MainController(mainView, inventoryController);

            container.Register<MainController>(mainController);
        }
    }
}
