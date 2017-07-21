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

        public async Task HandleAsync(CheckInItemsToInventory cmd)
        {
            var item = await this.repository.GetOrFailAsync<InventoryItem>(cmd.Id.ToString());
            if (cmd.Count <= 0)
                throw new InvalidCommandException("Must have a count greater than 0 to add to inventory", nameof(cmd.Count));
            item.Emit(new ItemsCheckedInToInventory(cmd.Id, cmd.Count));

            await this.repository.SaveAsync(item);
        }
    }
}
