namespace Inventory.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceLocator.Initialize();

            var mainController = ServiceLocator.ResolveSingleton<MainController>();
            mainController.StartCommandLoop();
        }
    }
}
