using Eventing.Core.Domain;

namespace Inventory.Domain
{
    [StreamCategory("myCompany.inventoryItems")]
    public class InventoryItem : EventSourced
    {
        public InventoryItem()
        {
            this.On<InventoryItemCreated>(e => this.StreamName = e.Id.ToString());
        }
    }

    public class InventoryItemSnapshot : Snapshot
    {
        public InventoryItemSnapshot(string streamName, int version) : base(streamName, version)
        {
        }
    }
}
