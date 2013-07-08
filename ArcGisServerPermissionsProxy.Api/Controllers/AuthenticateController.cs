using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AgrcPasswordManagement.Commands;
using AgrcPasswordManagement.Models.Account;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using CommandPattern;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class AuthenticateController : RavenApiController
    {
        public async Task<HttpResponseMessage> Post(LoginCredentials login)
        {
            TokenModel token;

            using (var s = AsyncSession)
            {
                var items = await s.Query<User, UserByEmailIndex>()
                                   .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                   .Customize(x => x.Include<Application>(o => o.Name))
                                   .Where(x => x.Email == login.Email && x.Application == login.ApplicationName)
                                   .ToListAsync();

                if (items == null || items.Count != 1)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound,
                                                  new ResponseContainer((int) HttpStatusCode.NotFound, "User not found."));
                }

                User user;
                try
                {
                    user = items.Single();
                }
                catch (InvalidOperationException)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound,
                                                  new ResponseContainer((int) HttpStatusCode.NotFound, "User not found."));
                }


                var valid = await CommandExecutor.ExecuteCommand(
                    new ValidateUserPasswordCommand(login.Password,
                                                    new ValidateLoginCredentials(user.Password, user.Salt, App.Pepper,
                                                                                 user.Id)));

                if (!valid)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer((int) HttpStatusCode.Unauthorized,
                                                                        "Your password does not match our records."));
                }

                if (user.Application != login.ApplicationName)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer((int) HttpStatusCode.Unauthorized,
                                                                        string.Format("You do not have access to {0}.",
                                                                                      login.ApplicationName)));
                }

                var application = await s.LoadAsync<Application>(user.Application);

                token = await CommandExecutor.ExecuteCommandAsync(
                    new GetTokenCommand(new GetTokenCommand.GetTokenParams("localhost", "arcgis", false, 6080),
                                        new GetTokenCommand.Credentials(login.ApplicationName, login.RoleName,
                                                                        application.Password)));

                if (!token.Successful)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer((int)HttpStatusCode.Unauthorized,
                                                                        token.Error.Message));
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK,
                                          new ResponseContainer<AuthenticationResponse>((int) HttpStatusCode.OK, null,
                                                                                        new AuthenticationResponse(
                                                                                            token.Token)));
        }
    }
}