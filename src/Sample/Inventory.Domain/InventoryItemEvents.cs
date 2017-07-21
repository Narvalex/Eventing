using System;

namespace Inventory.Domain
{
    public class InventoryItemCreated
    {
        public InventoryItemCreated(Guid id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }
    }

    public class ItemsCheckedInToInventory
    {
        public ItemsCheckedInToInventory(Guid id, int count)
        {
            this.Id = id;
            this.Count = count;
        }

        public Guid Id { get; }
        public int Count { get; }
    }
}
