using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Commands.Query;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using CommandPattern;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class UserController : RavenApiController
    {
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

            // does database exist
            if (Database == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Invalid application name."));
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
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                                               new ResponseContainer(HttpStatusCode.BadRequest,
                                                                     "Invalid application name."));
                }

                if (emailExists)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict,
                                              new ResponseContainer(HttpStatusCode.Conflict,
                                                                    "Duplicate user name."));
                }

                var password =
                    await CommandExecutor.ExecuteCommandAsync(new HashPasswordCommandAsync(user.Password, App.Pepper));

                var newUser = new User(user.Name, user.Email, user.Agency, password.HashedPassword, password.Salt,
                                       user.Application, null, null);

                await s.StoreAsync(newUser);

                var config = await s.LoadAsync<Config>("1");

                await Task.Factory.StartNew(() =>
                    {
                        CommandExecutor.ExecuteCommand(new NewUserAdminNotificationEmailCommand(
                                                           new NewUserAdminNotificationEmailCommand.MailTemplate(
                                                               config.AdministrativeEmails, new[] {"no-reply@utah.gov"},
                                                               user.Name, user.Agency,
                                                               null, user.Application, newUser.Token, config.Roles)));


                        CommandExecutor.ExecuteCommand(new UserRegistrationNotificationEmailCommand(
                                                           new UserRegistrationNotificationEmailCommand.MailTemplate(
                                                               new[] {user.Email}, config.AdministrativeEmails,
                                                               user.Name, user.Email, user.Application)));
                    });
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

                var password = CommandExecutor.ExecuteCommand(new GeneratePasswordCommand(12));

                var hashed =
                    await CommandExecutor.ExecuteCommandAsync(new HashPasswordCommandAsync(password, App.Pepper));

                user.Password = hashed.HashedPassword;
                user.Salt = hashed.Salt;

                await s.SaveChangesAsync();

                var config = await s.LoadAsync<Config>("1");

                await Task.Factory.StartNew(() =>
                                      CommandExecutor.ExecuteCommand(
                                          new PasswordResetEmailCommand(
                                              new PasswordResetEmailCommand.MailTemplate(new[] {user.Email},
                                                                                         config.AdministrativeEmails,
                                                                                         user.Name,
                                                                                         password, "url",
                                                                                         user.Application))));


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

        public class ChangePasswordRequestInformation : RequestInformation
        {
            public ChangePasswordRequestInformation(string email, string currentPassword, string newPassword,
                                                    string newPasswordRepeated, string application, Guid token)
                : base(application, token)
            {
                CurrentPassword = currentPassword;
                NewPassword = newPassword;
                NewPasswordRepeated = newPasswordRepeated;
                Email = email;
            }

            [Required]
            public string CurrentPassword { get; set; }

            [Required]
            public string NewPassword { get; set; }

            [Required]
            public string NewPasswordRepeated { get; set; }

            /// <summary>
            ///     Gets or sets the email.
            /// </summary>
            /// <value>
            ///     The email of the person to get the roles for.
            /// </value>
            [EmailAddress]
            public string Email { get; set; }
        }

        /// <summary>
        ///     A class encapsulating common request paramaters
        /// </summary>
        public class RequestInformation
        {
            private string _application;

            public RequestInformation()
            {
            }

            public RequestInformation(string application, Guid token)
            {
                Application = application;
                Token = token;
            }

            /// <summary>
            ///     Gets the database.
            /// </summary>
            /// <value>
            ///     The database or application name of the user.
            /// </value>
            [Required]
            public string Application
            {
                get { return _application == null ? null : _application.ToLowerInvariant(); }
                set
                {
                    if (value == null || string.IsNullOrEmpty(value))
                        _application = null;
                    else
                    {
                        _application = value;
                    }
                }
            }

            /// <summary>
            ///     Gets the token.
            /// </summary>
            /// <value>
            ///     The token arcgis server generated.
            /// </value>
            public Guid Token { get; protected set; }
        }

        /// <summary>
        ///     A class for reseting a users password
        /// </summary>
        public class ResetRequestInformation : RequestInformation
        {
            public ResetRequestInformation(string email, string application, Guid token)
                : base(application, token)
            {
                Email = email;
            }

            /// <summary>
            ///     Gets or sets the email.
            /// </summary>
            /// <value>
            ///     The email of the person to get the roles for.
            /// </value>
            [EmailAddress]
            public string Email { get; set; }
        }

        /// <summary>
        ///     A class for getting user role requests
        /// </summary>
        public class RoleRequestInformation : RequestInformation
        {
            public RoleRequestInformation()
            {
            }

            public RoleRequestInformation(string email, string application, Guid token)
                : base(application, token)
            {
                Email = email;
            }

            /// <summary>
            ///     Gets or sets the email.
            /// </summary>
            /// <value>
            ///     The email of the person to get the roles for.
            /// </value>
            [EmailAddress]
            public string Email { get; set; }
        }
    }
}