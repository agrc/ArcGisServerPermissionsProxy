using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Ninject;

namespace ArcGisServerPermissionsProxy.Api
{
    public class App : HttpApplication
    {
        public static IKernel Kernel { get; set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}