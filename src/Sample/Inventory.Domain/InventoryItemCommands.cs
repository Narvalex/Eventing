using System;

namespace Inventory.Domain
{
    public class CreateInventoryItem
    {
        public CreateInventoryItem(Guid id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }
    }

    public class CheckInItemsToInventory
    {
        public CheckInItemsToInventory(Guid id, int count)
        {
            this.Id = id;
            this.Count = count;
        }

        public Guid Id { get; }
        public int Count { get; }
    }
}
