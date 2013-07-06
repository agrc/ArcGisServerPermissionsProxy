using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AgrcPasswordManagement;
using AgrcPasswordManagement.Models.Account;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.Database;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using CommandPattern;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class AuthenticateController : RavenApiController
    {
        public async Task<HttpResponseMessage> Post(LoginCredentials login)
        {
            User user = null;

            using (var s = AsyncSession)
            {
                try
                {
                    user = s.Query<User, UserByEmailIndex>().Single(x => x.Email == login.Email && x.Application == login.AppId);
                }
                catch (InvalidOperationException ex)
                {
                    //user doesn't exist for this application
                }
            }

            if (user == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound,
                                   new ResponseContainer((int)HttpStatusCode.NotFound, "User not found."));
            }

            var valid = await CommandExecutor.ExecuteCommand(new ValidateUserPasswordCommand(login.Password,
                                                                               new ValidateLoginCredentials(
                                                                                   user.Password, user.Salt, user.Id)));
            if (!valid)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                    new ResponseContainer((int)HttpStatusCode.NotFound, "Your password does not match our records."));
            }

            return Request.CreateResponse(HttpStatusCode.OK,
                                   new ResponseContainer<AuthenticationResponse>((int)HttpStatusCode.OK, null,
                                                                                 new AuthenticationResponse("token")));
        }
    }
}