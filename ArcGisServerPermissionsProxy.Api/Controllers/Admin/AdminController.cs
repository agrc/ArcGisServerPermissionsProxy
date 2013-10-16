using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Commands.Query;
using ArcGisServerPermissionsProxy.Api.Commands.Users;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Models.Response.Account;
using ArcGisServerPermissionsProxy.Api.Raven.Configuration;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using CommandPattern;
using Ninject;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Controllers.Admin
{
    public class AdminController : RavenApiController
    {
        [Inject]
        public BootstrapArcGisServerSecurityCommandAsync BootstrapCommand { get; set; }

        [Inject]
        public IDatabaseExists DatabaseExists { get; set; }

        [Inject]
        public IIndexable IndexCreation { get; set; }

        [HttpPost]
        public async Task<HttpResponseMessage> CreateApplication(CreateApplicationParams parameters)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            if (ConfigurationManager.AppSettings["creationToken"] != parameters.CreationToken)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var database = parameters.Application;
            Database = database;

            using (var s = Session)
            {
                DatabaseExists.Esure(DocumentStore, Database);

                var catalog = new AssemblyCatalog(typeof (UserByEmailIndex).Assembly);
                var provider = new CatalogExportProvider(catalog)
                    {
                        SourceProvider = new CatalogExportProvider(catalog)
                    };

                IndexCreation.CreateIndexes(provider, DocumentStore, Database);

                if (!parameters.Roles.Select(x => x.ToLower()).Contains("admin"))
                {
                    parameters.Roles.Add("admin");
                }

                var config = s.Load<Config>("1");
                if (config == null)
                {
                    config = new Config(parameters.AdminEmails, parameters.Roles);

                    s.Store(config, "1");
                }

                foreach (var useremail in parameters.AdminEmails)
                {
                    if (s.Query<User, UserByEmailIndex>()
                         .Any(x => x.Email == useremail))
                    {
                        continue;
                    }

                    var password = CommandExecutor.ExecuteCommand(new GeneratePasswordCommand(12));

                    var hashed = await
                                 CommandExecutor.ExecuteCommandAsync(new HashPasswordCommandAsync(password, App.Pepper));

                    var adminUser = new User("admin", useremail, "", hashed.HashedPassword, hashed.Salt,
                                             database, "admin", "admintoken")
                        {
                            Active = true,
                            Approved = true
                        };

                    s.Store(adminUser);

                    await Task.Factory.StartNew(() =>
                                                CommandExecutor.ExecuteCommand(
                                                    new PasswordResetEmailCommand(
                                                        new PasswordResetEmailCommand.MailTemplate(
                                                            new[] {adminUser.Email},
                                                            config.AdministrativeEmails,
                                                            adminUser.Name,
                                                            password, "url",
                                                            parameters.Application))));
                }

                s.SaveChanges();
            }

            //add admin email to admin group and send email to reset password.
            BootstrapCommand.Parameters = parameters;
            BootstrapCommand.AdminInformation = App.AdminInformation;

            var messages = await CommandExecutor.ExecuteCommandAsync(BootstrapCommand);

            if (messages.Any())
            {
                return Request.CreateResponse(HttpStatusCode.Created,
                                              new ResponseContainer(HttpStatusCode.Created,
                                                                    string.Join(" ", messages.Select(x => x))));
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

            if(string.IsNullOrEmpty(info.AdminToken) || !info.AdminToken.Contains("."))
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                              new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                    "Bad Token."));
            }

            Database = info.Application;

            using (var s = AsyncSession)
            {
                var adminTokenParts = info.AdminToken.Split('.');
                if(adminTokenParts.Length != 2)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                              new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                    "Bad Token."));
                }

                var adminUser = await s.LoadAsync<User>(adminTokenParts[0]);
                if (adminUser.AdminToken != info.AdminToken)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                             new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                   "Bad Token."));
                }

                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserCommandAsync(info.Email, s));

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                                  new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                        "User not found."));
                }

                var response =
                    await CommandExecutor.ExecuteCommandAsync(new AcceptUserCommandAsync(s, info, Request, user));

                if (response != null)
                {
                    return response;
                }

                return Request.CreateResponse(HttpStatusCode.Accepted);
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Accept(string application, string role, Guid token)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(application) || string.IsNullOrEmpty(role) ||
                token == Guid.Empty)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            Database = application;

            using (var s = AsyncSession)
            {
                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserByTokenCommandAsync(token, s));

                if (user.Token != token)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                                                  new ResponseContainer(HttpStatusCode.BadRequest, "Incorrect token."));
                }

                if (user.ExpirationDateTicks < DateTime.Now.Ticks)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                                                  new ResponseContainer(HttpStatusCode.BadRequest,
                                                                        "This token has expired after one month of inactivity."));
                }

                var info = new AcceptRequestInformation(user.Email, role, token, application, null);

                var response =
                    await CommandExecutor.ExecuteCommandAsync(new AcceptUserCommandAsync(s, info, Request, user));

                if (response != null)
                {
                    return response;
                }

                return Request.CreateResponse(HttpStatusCode.OK, "Done.", new TextPlainResponseFormatter());
            }
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

            if (string.IsNullOrEmpty(info.AdminToken) || !info.AdminToken.Contains("."))
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                              new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                    "Bad Token."));
            }

            Database = info.Application;

            using (var s = AsyncSession)
            {
                var adminTokenParts = info.AdminToken.Split('.');
                if (adminTokenParts.Length != 2)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                              new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                    "Bad Token."));
                }

                var adminUser = await s.LoadAsync<User>(adminTokenParts[0]);
                if (adminUser.AdminToken != info.AdminToken)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                             new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                   "Bad Token."));
                }

                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserCommandAsync(info.Email, s));

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                                  new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                        "User not found."));
                }

                await CommandExecutor.ExecuteCommandAsync(new RejectUserCommandAsync(s, user));

                return Request.CreateResponse(HttpStatusCode.Accepted);
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Reject(string application, Guid token)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(application) || token == Guid.Empty)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            Database = application;

            using (var s = AsyncSession)
            {
                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserByTokenCommandAsync(token, s));

                if (user.Token != token)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                                                  new ResponseContainer(HttpStatusCode.BadRequest, "Incorrect token."));
                }

                if (user.ExpirationDateTicks < DateTime.Now.Ticks)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                                                  new ResponseContainer(HttpStatusCode.BadRequest,
                                                                        "This token has expired after one month of inactivity."));
                }

                await CommandExecutor.ExecuteCommandAsync(new RejectUserCommandAsync(s, user));

                return Request.CreateResponse(HttpStatusCode.OK, "Done.", new TextPlainResponseFormatter());
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetAllWaiting(string application)
        {
            if (!ValidationService.IsValid(application))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            Database = application;

            using (var s = AsyncSession)
            {
                var waitingUsers = await s.Query<User, UsersByApprovedIndex>()
                                          .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                          .Where(x => x.Active && x.Approved == false)
                                          .ToListAsync();

                return Request.CreateResponse(HttpStatusCode.OK,
                                              new ResponseContainer<IList<UsersWaiting>>(
                                                  waitingUsers.Select(x => new UsersWaiting(x)).ToList()));
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetRole(string email, string application)
        {
            if (string.IsNullOrEmpty(email) || !ValidationService.IsValid(application))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            Database = application;

            using (var s = AsyncSession)
            {
                var users = await s.Query<User, UserByEmailIndex>()
                                   .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                   .Where(x => x.Email == email.ToLowerInvariant())
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
                                              new ResponseContainer<string>(user.Role));
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetRoles(string application)
        {
            if (!ValidationService.IsValid(application))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            Database = application;

            using (var s = AsyncSession)
            {
                var conf = await s.LoadAsync<Config>("1");

                return Request.CreateResponse(HttpStatusCode.OK,
                                              new ResponseContainer<string[]>(conf.Roles));
            }
        }

        /// <summary>
        ///     A class for accepting users in the application
        /// </summary>
        public class AcceptRequestInformation : UserController.RequestInformation
        {
            public AcceptRequestInformation()
            {
            }

            public AcceptRequestInformation(string email, string role, Guid token, string application, string adminToken)
                : base(application, token)
            {
                Email = email;
                AdminToken = adminToken;
                Role = role == null ? "" : role.ToLowerInvariant();
            }

            /// <summary>
            ///     Gets or sets the email.
            /// </summary>
            /// <value>
            ///     The email of the person to get the roles for.
            /// </value>
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string AdminToken { get; set; }

            /// <summary>
            ///     Gets or sets the roles.
            /// </summary>
            /// <value>
            ///     The roles.
            /// </value>
            [Required]
            public string Role { get; set; }
        }

        public class CreateApplicationParams
        {
            [Required]
            public string Application { get; set; }

            [Required]
            public string[] AdminEmails { get; set; }

            [Required]
            public Collection<string> Roles { get; set; }

            [Required]
            public string CreationToken { get; set; }
        }

        /// <summary>
        ///     A class for getting user role requests
        /// </summary>
        public class RejectRequestInformation : UserController.RequestInformation
        {
            public RejectRequestInformation(string email, Guid token, string application, string adminToken)
                : base(application, token)
            {
                Email = email;
                AdminToken = adminToken;
            }

            /// <summary>
            ///     Gets or sets the email.
            /// </summary>
            /// <value>
            ///     The email of the person to get the roles for.
            /// </value>
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string AdminToken { get; set; }
        }
    }
}