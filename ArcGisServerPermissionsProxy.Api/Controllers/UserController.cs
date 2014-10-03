using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionProxy.Domain;
using ArcGisServerPermissionProxy.Domain.Account;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Commands.Query;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Configuration;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Services;
using CommandPattern;
using Ninject;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class UserController : RavenApiController
    {
        [Inject]
        public IUrlBuilder UrlBuilder { get; set; }

        [HttpPost]
        public async Task<HttpResponseMessage> Register(Credentials user)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            Database = user.Application;

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

                if (emailExists)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict,
                                                  new ResponseContainer(HttpStatusCode.Conflict,
                                                                        "Duplicate user name."));
                }

                var password =
                    await CommandExecutor.ExecuteCommandAsync(new HashPasswordCommandAsync(user.Password, App.Pepper));

                var newUser = new User(user.First, user.Last, user.Email, user.Agency, password.HashedPassword,
                                       password.Salt,
                                       user.Application, null, null, user.AccessRules, user.Additional);

                await s.StoreAsync(newUser);

                var config = await s.LoadAsync<Config>("1");

                var url = UrlBuilder.CreateUrl(ControllerContext, App.Host, "Default", "AdminEmail");

                CommandExecutor.ExecuteCommand(new NewUserAdminNotificationEmailCommand(
                                                   new NewUserAdminNotificationEmailCommand.MailTemplate(
                                                       config.AdministrativeEmails, new[] {"no-reply@utah.gov"},
                                                       user.FullName, user.Agency,
                                                       url, user.Application, newUser.Token, config.Roles,
                                                       config.Description, config.AdminUrl)));


                CommandExecutor.ExecuteCommand(new UserRegistrationNotificationEmailCommand(
                                                   new UserRegistrationNotificationEmailCommand.MailTemplate(
                                                       new[] {user.Email}, config.AdministrativeEmails,
                                                       user.FullName, user.Email, config.Description)));
                
                await s.SaveChangesAsync();
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        [HttpPut]
        public async Task<HttpResponseMessage> ResetPassword(ResetRequestInformation info)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            Database = info.Application;

            using (var s = AsyncSession)
            {
                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserCommandAsync(info.Email, s));

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                                  new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                        "User not found."));
                }

                var password = CommandExecutor.ExecuteCommand(new GeneratePasswordCommand(12));

                var hashed =
                    await CommandExecutor.ExecuteCommandAsync(new HashPasswordCommandAsync(password, App.Pepper));

                user.Password = hashed.HashedPassword;
                user.Salt = hashed.Salt;

                await s.SaveChangesAsync();

                var config = await s.LoadAsync<Config>("1");

                CommandExecutor.ExecuteCommand(new PasswordResetEmailCommand(new PasswordResetEmailCommand.MailTemplate(
                                                                                 new[] {user.Email},
                                                                                 new[] {"noreply@utah.gov"},
                                                                                 user.FullName,
                                                                                 password,
                                                                                 config.Description)));

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }

        [HttpPut]
        public async Task<HttpResponseMessage> ChangePassword(ChangePasswordRequestInformation info)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            if (info.NewPassword != info.NewPasswordRepeated)
            {
                return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                              new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                    "New passwords do not match."));
            }

            Database = info.Application;

            User user = null;
            using (var s = AsyncSession)
            {
                try
                {
                    user = await CommandExecutor.ExecuteCommandAsync(new GetUserCommandAsync(info.Email, s));
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

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                                  new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                        "User not found."));
                }

                var currentPassword = await
                                      CommandExecutor.ExecuteCommandAsync(
                                          new HashPasswordCommandAsync(info.CurrentPassword, user.Salt,
                                                                       App.Pepper));

                if (currentPassword.HashedPassword != user.Password)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                                  new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                        "Current password is incorrect."));
                }

                var newPassword = await
                                  CommandExecutor.ExecuteCommandAsync(
                                      new HashPasswordCommandAsync(info.NewPassword, user.Salt,
                                                                   App.Pepper));

                user.Password = newPassword.HashedPassword;
                user.Salt = newPassword.Salt;

                await s.SaveChangesAsync();
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        [HttpPut]
        public async Task<HttpResponseMessage> ChangeEmail(ChangeEmailRequestInformation info)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            if (info.Email == info.NewEmail)
            {
                return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                              new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                    "New email cannot match."));
            }

            Database = info.Application;

            User user = null;
            using (var s = AsyncSession)
            {
                try
                {
                    user = await CommandExecutor.ExecuteCommandAsync(new GetUserCommandAsync(info.Email, s));
                }
                catch (AggregateException aex)
                {
                    aex.Flatten().Handle(ex =>
                        {
                            if (ex is WebException)
                            {
                                Database = null;
                                return true;
                            }

                            return false; 
                        });
                }

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                                  new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                        "User not found."));
                }

                var currentPassword = await
                                      CommandExecutor.ExecuteCommandAsync(
                                          new HashPasswordCommandAsync(info.Password, user.Salt,
                                                                       App.Pepper));

                if (currentPassword.HashedPassword != user.Password)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                                  new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                        "Current password is incorrect."));
                }

                var isUnique = await s.Query<User, UserByEmailIndex>()
                                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                            .Where(x => x.Email == info.NewEmail.ToLowerInvariant())
                                            .CountAsync();

                if (isUnique > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                                  new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                        "Email is in use."));
                }

                user.Email = info.NewEmail;

                await s.SaveChangesAsync();
            }

            return Request.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}