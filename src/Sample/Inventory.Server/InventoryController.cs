using Inventory.Common;
using Inventory.Domain;
using System.Threading.Tasks;
using System.Web.Http;

namespace Inventory.Server
{
    [RoutePrefix(Endpoints.InventoryItems.Prefix)]
    public class InventoryController : ApiController
    {
        private readonly InventoryItemService service = ServiceLocator.ResolveSingleton<InventoryItemService>();

        [HttpPost]
        [Route(Endpoints.InventoryItems.CreateNewInventoryItem)]
        public async Task<IHttpActionResult> CreateNewInventoryItem(CreateInventoryItem cmd)
        {
            await this.service.HandleAsync(cmd);
            return this.Ok();
        }
    }
}
