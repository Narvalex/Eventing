using Inventory.ConsoleApp.Common;
using System;

namespace Inventory.ConsoleApp
{
    public class MainController : ConsoleController
    {
        private readonly IConsoleController[] controllers;

        public MainController(params IConsoleController[] controllers)
            : base(key: "help", description: "Shows the available commands")
        {
            this.controllers = controllers;
        }

        public void StartCommandLoop()
        {
            do
            {
                Console.WriteLine("Type help to see available commands");
                var cmd = Console.ReadLine();
                if (this.TryHandle(cmd)) continue;
                foreach (var controller in this.controllers)
                {
                    if (controller.TryHandle(cmd))
                        continue;
                }
            } while (true);
        }

        public override void Handle(string[] args)
        {
            Console.WriteLine("This is the current list of available commands");
            foreach (var controller in this.controllers)
                Console.WriteLine(controller.Description);
        }
    }
}
