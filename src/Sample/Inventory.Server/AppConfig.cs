using System.Configuration;

namespace Inventory.Server
{
    public static class AppConfig
    {
        public static bool RunInMemory
        {
            get
            {
                var response = int.Parse(ConfigurationManager.AppSettings["runInMemory"]); // values 0: false, > 0 == TRUE
                return response > 0;
            }
        }

        public static string ServerBaseUrl
        {
            get
            {
                return ConfigurationManager.AppSettings["serverBaseUrl"];
            }
        }
    }
}
