using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AgrcPasswordManagement.Models.Account;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.Response;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class AuthenticateController : RavenApiController
    {
        public async Task<HttpResponseMessage> Post(LoginCredentials user)
        {
            return Request.CreateResponse(HttpStatusCode.OK,
                                   new ResponseContainer<AuthenticationResponse>((int)HttpStatusCode.OK, null,
                                                                                 new AuthenticationResponse("token")));
        }
    }
}