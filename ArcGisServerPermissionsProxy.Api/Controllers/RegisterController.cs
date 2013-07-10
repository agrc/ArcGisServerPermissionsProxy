using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using CommandPattern;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class RegisterController : RavenApiController
    {
        [HttpPost]
        public async Task<HttpResponseMessage> CreateNewUser(Credentials user)
        {
            Database = user.Database;

            // does database exist
            if (Database == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "12");
            }

            using (var s = AsyncSession)
            {
                //does username exist

                var emailExists = false;

                try
                {
                    emailExists = await s.Query<User, UserByEmailIndex>()
                                         .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                         .Where(x => x.Email == user.Email)
                                         .AnyAsync();
                }
                catch (AggregateException aex)
                {
                    aex.Flatten().Handle(ex => // Note that we still need to call Flatten
                        {
                            if (ex is WebException)
                            {
                                Database = null;
                                return true;
                            }

                            return false; // All other exceptions will get rethrown
                        });
                }

                if (Database == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "12");
                }

                if (emailExists)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "6");
                }

                var password =
                    await CommandExecutor.ExecuteCommandAsync(new HashPasswordCommandAsync(user.Password, App.Pepper));

                var newUser = new User(user.Email, password.HashedPassword, password.Salt, user.ApplicationName,
                                       Enumerable.Empty<string>());

                await s.StoreAsync(newUser);

                CommandExecutor.ExecuteCommand(new NewUserNotificationEmailCommand(
                                                   new NewUserNotificationEmailCommand.NewUserNotificationTemplate(
                                                       "sgourley@utah.gov", user.Name, user.Agency, user.ApplicationName,
                                                       "url")));
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }
    }
}