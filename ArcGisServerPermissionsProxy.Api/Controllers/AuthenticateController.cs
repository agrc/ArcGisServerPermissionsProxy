using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AgrcPasswordManagement.Commands;
using AgrcPasswordManagement.Models.Account;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Models.Response.Authentication;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using ArcGisServerPermissionsProxy.Api.Services;
using CommandPattern;
using Ninject;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class AuthenticateController : RavenApiController
    {
        [Inject]
        public ITokenService TokenService { get; set; }

        [HttpPost]
        public async Task<HttpResponseMessage> User(LoginCredentials login)
        {
            TokenModel token;
            Database = login.Application;

            using (var s = AsyncSession)
            {
                var items = await s.Query<User, UserByEmailIndex>()
                                   .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                   .Where(x => x.Email == login.Email)
                                   .ToListAsync();

                if (items == null || items.Count != 1)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound,
                                                  new ResponseContainer(HttpStatusCode.NotFound, "User not found."));
                }

                User user;
                try
                {
                    user = items.Single();
                }
                catch (InvalidOperationException)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound,
                                                  new ResponseContainer(HttpStatusCode.NotFound, "User not found."));
                }


                var valid = await CommandExecutor.ExecuteCommand(
                    new ValidateUserPasswordCommand(login.Password,
                                                    new ValidateLoginCredentials(user.Password, user.Salt, App.Pepper,
                                                                                 user.Id)));

                if (!valid)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        "Your password does not match our records."));
                }

                if (user.Application != login.Application)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        string.Format("You do not have access to {0}.",
                                                                                      login.Application)));
                }

                token = await TokenService.GetToken(new GetTokenCommandAsync.GetTokenParams("localhost", "arcgis", false, 6080),
                                                    new GetTokenCommandAsync.Credentials(login.Application, login.Role, App.Password));

                if (!token.Successful)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        token.Error.Message));
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, new ResponseContainer<AuthenticationResponse>(new AuthenticationResponse(token.Token)));
        }
    }
}