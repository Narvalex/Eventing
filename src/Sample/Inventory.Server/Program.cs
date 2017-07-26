using Eventing.GetEventStore;
using Eventing.Log;
using Microsoft.Owin.Hosting;
using System;
using System.Runtime.InteropServices;

namespace Inventory.Server
{
    class Program
    {
        private static ILogLite _log = LogManager.GlobalLogger;
        private static bool _runInMemory;
        #region Extern References
        // Source: http://stackoverflow.com/questions/474679/capture-console-exit-c-sharp
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ExtConsoleHandler handler, bool add);
        private delegate bool ExtConsoleHandler(CtrlType signal);

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        #endregion

        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(
              add: true,
              handler: signal =>
              {
                  CleanUp();
                  // Shutdown right away
                  Environment.Exit(-1);
                  return true;
              });

            var log = _log;
            log.Info("Starting Inventory Server");

            ServiceLocator.Initialize();
            log.Verbose("All dependencies were resolved successfully");

            _runInMemory = AppConfig.RunInMemory;
            if (_runInMemory)
                RunInMemory();
            else
                RunInEventStore();

            RunWebServer();
            RunCommandLoop();
            CleanUp();
        }

        static void RunCommandLoop()
        {
            string line;
            do
            {
                Console.WriteLine("Type exit to shut down");
                line = Console.ReadLine();
            }
            while (!line.Equals("exit", StringComparison.InvariantCultureIgnoreCase));
        }

        static void RunWebServer()
        {
            var log = _log;
            log.Verbose("Starting web server");

            var baseUrl = AppConfig.ServerBaseUrl;
            WebApiStartup.OnAppDisposing = CleanUp;
            WebApp.Start<WebApiStartup>(baseUrl);
            log.Verbose("Web server is running at " + baseUrl);
        }

        static void RunInMemory()
        {
            var log = _log;
            log.Info("Runing IN MEMORY");
        }

        static void RunInEventStore()
        {
            var log = _log;
            log.Info("Runing IN EVENT STORE");
            var esm = ServiceLocator.ResolveSingleton<EventStoreManager>();
#if DROP_DB
            log.Warning("Executing DROP AND CRATE EventStore");
            esm.DropAndCreateDb();
#endif
#if !DROP_DB
            log.Info("Executing CREATE IF NOT EXISTS EventStore");
            esm.CreateDbIfNotExists();
#endif
        }

        static void CleanUp()
        {
            if (_runInMemory)
                return;
            else
                ServiceLocator.ResolveSingleton<EventStoreManager>()
                    .TearDown();
        }
    }
}
