using System.Web.Http.Controllers;

namespace ArcGisServerPermissionsProxy.Api.Services {

    public class MockUrlBuilder : IUrlBuilder {
        public string CreateUrl(HttpControllerContext controller, string host, string routeName, string controllerName)
        {
            return "";
        }
    }

}