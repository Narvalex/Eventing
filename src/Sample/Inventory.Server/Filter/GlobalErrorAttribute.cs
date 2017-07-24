using Eventing.Log;
using System.Web.Http.Filters;

namespace Inventory.Server.Filter
{
    public class GlobalErrorAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            LogManager.GlobalLogger.Error(actionExecutedContext.Exception, $"Error en: {actionExecutedContext.Request.RequestUri}");
        }
    }
}
