using System.Configuration;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using Ninject;

namespace ArcGisServerPermissionsProxy.Api
{
    public class App : HttpApplication
    {
        public static IKernel Kernel { get; set; }

        public static string Pepper
        {
            get { return ")(*&(*^%*&^$*^#$"; }
        }

        public static string Password { get; set; }

        public static AdminCredentials AdminInformation { get; set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            AdminInformation = new AdminCredentials(ConfigurationManager.AppSettings["adminUserName"],
                                         ConfigurationManager.AppSettings["adminPassword"]);

            Password = ConfigurationManager.AppSettings["accountPassword"];
        }
    }
}