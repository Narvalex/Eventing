using Eventing;
using Eventing.Log;
using Inventory.ConsoleApp.Common;
using System;

namespace Inventory.ConsoleApp
{
    public class MainController : ConsoleController
    {
        private readonly ILogLite log = LogManager.GetLoggerFor<MainController>();
        private readonly IConsoleController[] controllers;
        private readonly MainView mainView;

        public MainController(MainView mainView, params IConsoleController[] controllers)
            : base(key: "help", description: "Shows the available commands")
        {
            Ensure.NotNull(mainView, nameof(mainView));

            this.controllers = controllers;
            this.mainView = mainView;
        }

        public void StartCommandLoop()
        {
            do
            {
                var cmd = this.mainView.GetUserCommand();
                if (this.TryHandle(cmd)) continue;
                if (cmd.Equals("exit", StringComparison.InvariantCultureIgnoreCase)) return;
                foreach (var controller in this.controllers)
                {
                    try
                    {
                        if (controller.TryHandle(cmd))
                            continue;
                    }
                    catch (Exception ex)
                    {
                        this.log.Error(ex, "An unhanled exception ocurred");
                    }
                }
            } while (true);
        }

        public override void Handle(string[] args)
        {
            this.mainView.ShowAvailableCommands(this.controllers);
        }
    }
}
