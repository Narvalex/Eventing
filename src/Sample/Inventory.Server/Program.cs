using Eventing.Log;

namespace Inventory.Server
{
    class Program
    {
        private static ILogLite _log = LogManager.GlobalLogger;

        static void Main(string[] args)
        {
            _log.Info("Starting Inventory Server");
        }
    }
}
