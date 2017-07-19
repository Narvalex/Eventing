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
}
