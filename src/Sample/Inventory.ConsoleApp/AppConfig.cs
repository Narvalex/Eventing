using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.ConsoleApp
{
    public static class AppConfig
    {
        public static string ServerBaseUrl
        {
            get
            {
                return ConfigurationManager.AppSettings["serverBaseUrl"];
            }
        }
    }
}
