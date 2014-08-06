using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class VersionController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var vString = version.ToString();

            return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    versionDetails = version, 
                    version = vString
                });
        }
    }
}
