using System.Web.Http;

namespace ArcGisServerPermissionsProxy.Controllers
{
    public class AuthenticateController : ApiController
    {
        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody] string value)
        {
        }
    }
}