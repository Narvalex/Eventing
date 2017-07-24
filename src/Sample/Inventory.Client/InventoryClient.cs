using Eventing.Client.Http;
using Inventory.Common;
using Inventory.Domain;
using System;
using System.Threading.Tasks;

namespace Inventory.Client
{
    public class InventoryClient : ClientBase
    {
        public InventoryClient(HttpLite http, Func<string> tokenProvider = null)
            : base(http, tokenProvider, Endpoints.InventoryItems.Prefix)
        {
        }

        public async Task CreateNewInventoryItem(string name)
        {
            var command = new CreateInventoryItem(Guid.NewGuid(), name);
            await base.Post(Endpoints.InventoryItems.CreateNewInventoryItem, command);
        }
    }
}
