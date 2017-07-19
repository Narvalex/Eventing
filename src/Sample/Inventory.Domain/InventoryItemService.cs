using Eventing.Core.Domain;
using System.Threading.Tasks;

namespace Inventory.Domain
{
    public class InventoryItemService : EventSourcedService
    {
        public InventoryItemService(IEventSourcedRepository repository) : base(repository)
        {
        }

        public async Task HandleAsync(CreateInventoryItem cmd)
        {
            var item = new InventoryItem();
            item.Emit(new InventoryItemCreated(cmd.Id, cmd.Name));

            await this.repository.SaveAsync(item);
        }
    }
}
