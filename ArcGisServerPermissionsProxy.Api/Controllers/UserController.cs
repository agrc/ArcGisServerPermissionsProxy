using System;
using System.Collections.Generic;
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

                Task.Factory.StartNew(() =>
                    {
                        CommandExecutor.ExecuteCommand(new NewUserNotificationEmailCommand(
                                                           new NewUserNotificationEmailCommand.MailTemplate(
                                                               new[] {""}, new[] {""}, user.Name, user.Agency,
                                                               null, user.ApplicationName)));


                        CommandExecutor.ExecuteCommand(new UserRegistrationNotificationEmailCommand(
                                                           new UserRegistrationNotificationEmailCommand.MailTemplate(
                                                               new[] {user.Email}, new[] {""}, user.Name, user.Email, user.ApplicationName)));
                    });
            }

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        [HttpPut]
        public async Task<HttpResponseMessage> Accept(AcceptRequestInformation info)
        {
            Database = info.Database;

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

                user.Active = true;
                user.Approved = true;
                user.Roles = info.Roles;

                await s.SaveChangesAsync();

                Task.Factory.StartNew(
                    () =>
                    CommandExecutor.ExecuteCommand(
                        new UserAcceptedEmailCommand(new UserAcceptedEmailCommand.MailTemplate(new[] { user.Email }, new[] { "" }, 
                            user.Name, info.Roles, user.Email, user.Application))));

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }

        [HttpDelete]
        public async Task<HttpResponseMessage> Reject(RejectRequestInformation info)
        {
            Database = info.Database;

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

                user.Active = false;
                user.Approved = false;
                user.Roles = new string[0];

                await s.SaveChangesAsync();

                Task.Factory.StartNew(() =>
                                      CommandExecutor.ExecuteCommand(
                                          new UserRejectedEmailCommand(
                                              new UserRejectedEmailCommand.MailTemplate(new[] { user.Email }, new[] { "" }, user.Name,
                                                                                        user.Application))));


                return Request.CreateResponse(HttpStatusCode.Accepted);
            }
        }

        [HttpPut]
        public async Task<HttpResponseMessage> ResetPassword(ResetRequestInformation info)
        {
            Database = info.Database;

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

                var password = CommandExecutor.ExecuteCommand(new GeneratePasswordCommand(12));

                var hashed =
                    await CommandExecutor.ExecuteCommandAsync(new HashPasswordCommandAsync(password, App.Pepper));

                user.Password = hashed.HashedPassword;
                user.Salt = hashed.Salt;

                await s.SaveChangesAsync();

                Task.Factory.StartNew(() =>
                                      CommandExecutor.ExecuteCommand(
                                          new PasswordResetEmailCommand(
                                              new PasswordResetEmailCommand.MailTemplate(new[] { user.Email }, new[] { "" }, user.Name,
                                                                                         password,"url", user.Application))));


                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetAllWaiting(RequestInformation info)
        {
            Database = info.Database;

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
            Database = info.Database;

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
            public AcceptRequestInformation(string database, string token, string email, IEnumerable<string> roles)
                : base(database, token)
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
            public string Email { get; set; }

            /// <summary>
            ///     Gets or sets the roles.
            /// </summary>
            /// <value>
            ///     The roles.
            /// </value>
            public string[] Roles { get; set; }
        }

        /// <summary>
        ///     A class for getting user role requests
        /// </summary>
        public class RejectRequestInformation : RequestInformation
        {
            public RejectRequestInformation(string database, string token, string email)
                : base(database, token)
            {
                Email = email;
            }

            /// <summary>
            ///     Gets or sets the email.
            /// </summary>
            /// <value>
            ///     The email of the person to get the roles for.
            /// </value>
            public string Email { get; set; }
        }

        /// <summary>
        ///     A class encapsulating common request paramaters
        /// </summary>
        public class RequestInformation
        {
            private string _database;

            public RequestInformation(string database, string token)
            {
                Database = database;
                Token = token;
            }

            /// <summary>
            ///     Gets the database.
            /// </summary>
            /// <value>
            ///     The database or application name of the user.
            /// </value>
            public string Database
            {
                get { return _database; }
                private set
                {
                    if (value.ToLowerInvariant() == "system" || string.IsNullOrEmpty(value))
                        _database = null;
                    else
                    {
                        _database = value;
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
            public ResetRequestInformation(string database, string token, string email)
                : base(database, token)
            {
                Email = email;
            }

            /// <summary>
            ///     Gets or sets the email.
            /// </summary>
            /// <value>
            ///     The email of the person to get the roles for.
            /// </value>
            public string Email { get; set; }
        }

        /// <summary>
        ///     A class for getting user role requests
        /// </summary>
        public class RoleRequestInformation : RequestInformation
        {
            public RoleRequestInformation(string database, string token, string email)
                : base(database, token)
            {
                Email = email;
            }

            /// <summary>
            ///     Gets or sets the email.
            /// </summary>
            /// <value>
            ///     The email of the person to get the roles for.
            /// </value>
            public string Email { get; set; }
        }
    }
}