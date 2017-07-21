using Eventing.TestHelpers;

namespace Inventory.Domain.Tests
{
    public class InventoryItemSpec
    {
        protected TestableEventSourcedService<InventoryItemService> sut;

        public InventoryItemSpec()
        {
            this.sut = new TestableEventSourcedService<InventoryItemService>(r => new InventoryItemService(r));
        }
    }
}
