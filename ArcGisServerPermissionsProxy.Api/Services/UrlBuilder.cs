using System.Web.Http.Controllers;
using System.Web.Http.Routing;

namespace ArcGisServerPermissionsProxy.Api.Services {

    public class UrlBuilder : IUrlBuilder {
        public string CreateUrl(HttpControllerContext controller, string host, string routeName, string controllerName)
        {
            var urlBuilder = new UrlHelper(controller.Request);
            return string.Format("{0}{1}", App.Host, urlBuilder.Route(routeName, new
                {
                    Controller = controllerName
                }));
        }
    }

}