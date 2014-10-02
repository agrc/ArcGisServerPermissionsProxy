using System.Web.Http.Controllers;

namespace ArcGisServerPermissionsProxy.Api.Services {

    public interface IUrlBuilder {
        string CreateUrl(HttpControllerContext controller, string host, string routeName, string controllerName);
    }

}