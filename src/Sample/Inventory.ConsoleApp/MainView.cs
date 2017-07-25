using System;

namespace Inventory.ConsoleApp
{
    public class MainView
    {
        public string GetUserCommand()
        {
            Console.WriteLine("Type help to see available commands or exit to terminate");
            var cmd = Console.ReadLine();
            return cmd;
        }

        public void ShowAvailableCommands(IConsoleController[] controllers)
        {
            Console.WriteLine("This is the current list of available commands");
            foreach (var controller in controllers)
                Console.WriteLine($"[{controller.Key}]: {controller.Description}");
        }

        public void ShowCommandNotFound(string cmd)
        {
            Console.WriteLine($"The command {cmd} was not found. Try another one...");
        }
    }
}
