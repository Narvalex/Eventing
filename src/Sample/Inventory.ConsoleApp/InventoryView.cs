using System;

namespace Inventory.ConsoleApp
{
    public class InventoryView
    {
        internal string GetCommand()
        {
            Console.WriteLine("To add items type add");
            return Console.ReadLine().ToLowerInvariant();
        }

        public string GetItemNameForAdd()
        {
            Console.WriteLine("Please type the name of the new item:");
            return Console.ReadLine();
        }

        public void NowWeAre(string work)
        {
            Console.WriteLine($"Please wait. Now we are {work}");
        }
    }
}
