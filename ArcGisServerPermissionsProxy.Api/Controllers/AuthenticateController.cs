using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Security;
using AgrcPasswordManagement.Commands;
using AgrcPasswordManagement.Models.Account;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionProxy.Domain.Response.Authentication;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Services;
using ArcGisServerPermissionsProxy.Api.Services.Token;
using CommandPattern;
using Ninject;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class AuthenticateController : RavenApiController
    {
        [Inject]
        public ITokenService TokenService { get; set; }

        [HttpPost, ActionName("User")]
        public async Task<HttpResponseMessage> UserLogin(LoginCredentials login)
        {
            TokenModel token;
            Database = login.Application;

            User user;
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

                var config = await s.LoadAsync<Config>("1");
                if (config.UsersCanExpire)
                {
                    var today = DateTime.UtcNow.Date.Ticks;
                    if (user.AccessRules.StartDate == 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized, "Use restrictions are enabled but not setup for your account. Contact the administrators."));
                    }

                    if (user.AccessRules.StartDate > today)
                    {
                        return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        string.Format("You are are not authorized for use until {0}", new DateTime(user.AccessRules.StartDate, DateTimeKind.Utc).ToShortDateString())));
                    }

                    if (user.AccessRules.EndDate < today)
                    {
                        return Request.CreateResponse(HttpStatusCode.Unauthorized,
                          new ResponseContainer(HttpStatusCode.Unauthorized,
                                                string.Format("You were only authorized for use until {0}", new DateTime(user.AccessRules.EndDate).ToShortDateString())));

                    }

                }

                token = await TokenService.GetToken(
                    new GetTokenCommandAsyncBase.GetTokenParams("localhost", "arcgis", false, 6080),
                    new GetTokenCommandAsyncBase.User(null, App.Password), login.Application, user.Role);

                if (!token.Successful)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        token.Error.Message));
                }

                if (user.Role.Contains("admin"))
                {
                    user.AdminToken = string.Format("{0}.{1}", user.Id, Guid.NewGuid());
                }
            }

            var response = Request.CreateResponse(HttpStatusCode.OK,
                                                  new ResponseContainer<AuthenticationResponse>(
                                                      new AuthenticationResponse(token, user)));

            var formsAuth = new FormsAuthWrapper();
            var cookie = formsAuth.SetAuthCookie(user.Email, user.Application, login.Persist);

            response.Headers.AddCookies(new Collection<CookieHeaderValue> {cookie});

            return response;
        }

        [HttpGet]
        public HttpResponseMessage ForgetMe()
        {
            var formsAuth = new FormsAuthWrapper();
            formsAuth.SignOut();

            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [HttpGet, Authorize]
        public async Task<HttpResponseMessage> RememberMe()
        {
            TokenModel token;

            var username = User.Identity.Name;
            var application = "";

            foreach (var value in Request.Headers.GetCookies(".ASPXAUTH"))
            {
                var ticket = FormsAuthentication.Decrypt(value.Cookies.First().Value);
                application = ticket.UserData;
            }

            Database = application;

            User user;
            using (var s = AsyncSession)
            {
                var items = await s.Query<User, UserByEmailIndex>()
                                   .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                   .Where(x => x.Email == username)
                                   .ToListAsync();

                if (items == null || items.Count != 1)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound,
                                                  new ResponseContainer(HttpStatusCode.NotFound, "User not found."));
                }

                try
                {
                    user = items.Single();
                }
                catch (InvalidOperationException)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound,
                                                  new ResponseContainer(HttpStatusCode.NotFound, "User not found."));
                }

                if (user.Application != application)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        string.Format("You do not have access to {0}.",
                                                                                      application)));
                }

                token = await TokenService.GetToken(
                    new GetTokenCommandAsyncBase.GetTokenParams("localhost", "arcgis", false, 6080),
                    new GetTokenCommandAsyncBase.User(null, App.Password), application, user.Role);

                if (!token.Successful)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        token.Error.Message));
                }

                if (user.Role.Contains("admin"))
                {
                    user.AdminToken = string.Format("{0}.{1}", user.Id, Guid.NewGuid());
                }
            }

            var response = Request.CreateResponse(HttpStatusCode.OK,
                                                  new ResponseContainer<AuthenticationResponse>(
                                                      new AuthenticationResponse(token, user)));

            return response;
        }
    }
}