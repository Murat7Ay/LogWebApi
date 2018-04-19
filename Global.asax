using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using com.Telekurye.WebServices.LogWebApi.Handler;

namespace com.Telekurye.WebServices.LogWebApi
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //İntegrationId for constructor
            GlobalConfiguration.Configuration.MessageHandlers.Add(new TransactionLoggingHandler(14));
        }
    }
}
