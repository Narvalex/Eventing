using Eventing.TestHelpers;

namespace Inventory.Domain.Tests
{
    public abstract class InventoryItemSpec
    {
        protected TestableEventSourcedService<InventoryItemService> sut;

        public InventoryItemSpec()
        {
            this.sut = new TestableEventSourcedService<InventoryItemService>(r => new InventoryItemService(r));
        }
    }
}
