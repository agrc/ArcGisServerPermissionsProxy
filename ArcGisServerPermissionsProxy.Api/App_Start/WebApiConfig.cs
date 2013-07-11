using System.Web.Http;
using ArcGisServerPermissionsProxy.Api.Configuration.Ninject;
using Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}"
            );

            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            GlobalConfiguration.Configuration.DependencyResolver = new NinjectDependencyResolver(App.Kernel);

#if DEBUG
            config.EnableSystemDiagnosticsTracing();
#endif
        }
    }
}