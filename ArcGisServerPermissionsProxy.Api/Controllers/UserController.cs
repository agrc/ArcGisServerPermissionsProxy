using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
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

                var newUser = new User(user.Name, user.Email, user.Agency, password.HashedPassword, password.Salt,
                                       user.Application,
                                       Enumerable.Empty<string>());

                await s.StoreAsync(newUser);

                var config = await s.LoadAsync<Config>("1");

                Task.Factory.StartNew(() =>
                    {
                        CommandExecutor.ExecuteCommand(new NewUserNotificationEmailCommand(
                                                           new NewUserNotificationEmailCommand.MailTemplate(
                                                               config.AdministrativeEmails, new[] {"no-reply@utah.gov"},
                                                               user.Name, user.Agency,
                                                               null, user.Application)));


                        CommandExecutor.ExecuteCommand(new UserRegistrationNotificationEmailCommand(
                                                           new UserRegistrationNotificationEmailCommand.MailTemplate(
                                                               new[] {user.Email}, config.AdministrativeEmails,
                                                               user.Name, user.Email, user.Application)));
                    });
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        [HttpPut]
        public async Task<HttpResponseMessage> Accept(AcceptRequestInformation info)
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
                var user = await GetUser(info.Email, s);

                user.Active = true;
                user.Approved = true;
                user.Roles = info.Roles;

                await s.SaveChangesAsync();

                var config = await s.LoadAsync<Config>("1");

                Task.Factory.StartNew(
                    () =>
                    CommandExecutor.ExecuteCommand(
                        new UserAcceptedEmailCommand(new UserAcceptedEmailCommand.MailTemplate(new[] {user.Email},
                                                                                               config.
                                                                                                   AdministrativeEmails,
                                                                                               user.Name, info.Roles,
                                                                                               user.Email,
                                                                                               user.Application))));

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }

        private static async Task<User> GetUser(string email, IAsyncDocumentSession s)
        {
            var users = await s.Query<User, UserByEmailIndex>()
                               .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                               .Where(x => x.Email == email.ToLowerInvariant())
                               .ToListAsync();

            User user = null;
            try
            {
                user = users.Single();
            }
            catch (InvalidOperationException)
            {
                return user;
            }

            return user;
        }

        [HttpDelete]
        public async Task<HttpResponseMessage> Reject(RejectRequestInformation info)
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
                var user = await GetUser(info.Email, s);

                user.Active = false;
                user.Approved = false;
                user.Roles = new string[0];

                await s.SaveChangesAsync();

                var config = await s.LoadAsync<Config>("1");

                Task.Factory.StartNew(() =>
                                      CommandExecutor.ExecuteCommand(
                                          new UserRejectedEmailCommand(
                                              new UserRejectedEmailCommand.MailTemplate(new[] {user.Email},
                                                                                        config.AdministrativeEmails,
                                                                                        user.Name,
                                                                                        user.Application))));


                return Request.CreateResponse(HttpStatusCode.Accepted);
            }
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
                var user = await GetUser(info.Email, s);

                var password = CommandExecutor.ExecuteCommand(new GeneratePasswordCommand(12));

                var hashed =
                    await CommandExecutor.ExecuteCommandAsync(new HashPasswordCommandAsync(password, App.Pepper));

                user.Password = hashed.HashedPassword;
                user.Salt = hashed.Salt;

                await s.SaveChangesAsync();

                var config = await s.LoadAsync<Config>("1");

                Task.Factory.StartNew(() =>
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
                    user = await GetUser(info.Email, s);
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

        [HttpGet]
        public async Task<HttpResponseMessage> GetAllWaiting(RequestInformation info)
        {
            Database = info.Application;

            using (var s = AsyncSession)
            {
                var waitingUsers = await s.Query<User, UsersByApprovedIndex>()
                                          .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                          .Where(x => x.Active && x.Approved == false)
                                          .ToListAsync();

                return Request.CreateResponse(HttpStatusCode.OK,
                                              new ResponseContainer<IList<User>>(waitingUsers));
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetRoles(RoleRequestInformation info)
        {
            Database = info.Application;

            using (var s = AsyncSession)
            {
                var users = await s.Query<User, UserByEmailIndex>()
                                   .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                   .Where(x => x.Email == info.Email.ToLowerInvariant())
                                   .ToListAsync();

                User user;
                try
                {
                    user = users.Single();
                }
                catch (InvalidOperationException)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound,
                                                  new ResponseContainer(HttpStatusCode.NotFound, "User not found."));
                }

                return Request.CreateResponse(HttpStatusCode.OK,
                                              new ResponseContainer<IList<string>>(user.Roles));
            }
        }

        /// <summary>
        ///     A class for accepting users in the application
        /// </summary>
        public class AcceptRequestInformation : RequestInformation
        {
            public AcceptRequestInformation(string application, string token, string email, IEnumerable<string> roles)
                : base(application, token)
            {
                Email = email;
                Roles = roles.Select(x => x.ToLowerInvariant()).ToArray();
            }

            /// <summary>
            ///     Gets or sets the email.
            /// </summary>
            /// <value>
            ///     The email of the person to get the roles for.
            /// </value>
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     Gets or sets the roles.
            /// </summary>
            /// <value>
            ///     The roles.
            /// </value>
            [Required]
            public string[] Roles { get; set; }
        }

        public class ChangePasswordRequestInformation : RequestInformation
        {
            public ChangePasswordRequestInformation(string email, string currentPassword, string newPassword,
                                                    string newPasswordRepeated, string application, string token)
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
        ///     A class for getting user role requests
        /// </summary>
        public class RejectRequestInformation : RequestInformation
        {
            public RejectRequestInformation(string application, string token, string email)
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
        ///     A class encapsulating common request paramaters
        /// </summary>
        public class RequestInformation
        {
            private string _application;

            public RequestInformation(string application, string token)
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
                get { return _application; }
                private set
                {
                    if (value == null || value.ToLowerInvariant() == "system" || string.IsNullOrEmpty(value))
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
            public string Token { get; private set; }
        }

        /// <summary>
        ///     A class for reseting a users password
        /// </summary>
        public class ResetRequestInformation : RequestInformation
        {
            public ResetRequestInformation(string application, string token, string email)
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
            public RoleRequestInformation(string application, string token, string email)
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