namespace Inventory.Common
{
    public static class Endpoints
    {
        public static class InventoryItems
        {
            public const string Prefix = "inventory";
            public const string CreateNewInventoryItem = "new-inventory-item";

            public static class Query
            {
                public const string Prefix = "inventory/query";
                public const string GetAllItems = "all-items";
            }
        }
    }
}
