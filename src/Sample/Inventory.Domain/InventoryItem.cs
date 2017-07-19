using Eventing.Core.Domain;

namespace Inventory.Domain
{
    [StreamCategory("myCompany.inventoryItems")]
    public class InventoryItem : EventSourced
    {
    }
}
