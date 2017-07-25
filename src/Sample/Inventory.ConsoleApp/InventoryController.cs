using Eventing;
using Inventory.Client;
using Inventory.ConsoleApp.Common;

namespace Inventory.ConsoleApp
{
    public class InventoryController : ConsoleController
    {
        private readonly InventoryView view;
        private readonly InventoryClient client;

        public InventoryController(InventoryView view, InventoryClient client)
            : base(key: "inventory", description: "Manage the inventory system")
        {
            Ensure.NotNull(view, nameof(view));
            Ensure.NotNull(client, nameof(client));

            this.view = view;
            this.client = client;
        }

        public override void Handle(string[] args)
        {
            var cmd = this.view.GetCommand();
            switch (cmd)
            {
                case "add":
                    var itemName = this.view.GetItemNameForAdd();
                    this.view.NowWeAre("adding an item...");
                    this.client.CreateNewInventoryItem(itemName).Wait();
                    break;

                default:
                    break;
            }
        }
    }
}
