using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionProxy.Domain;
using ArcGisServerPermissionProxy.Domain.Account;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Commands.Query;
using ArcGisServerPermissionsProxy.Api.Commands.Users;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Configuration;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using CommandPattern;
using NLog;
using Newtonsoft.Json;
using Ninject;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Controllers.Admin {

    public class AdminController : RavenApiController {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
                var errors = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));

                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    string.Format("Missing parameters. {0}", errors)));
            }

            if (App.CreationToken != parameters.CreationToken)
            {
                Logger.Info("Token does not match {1}. Input Params: {0}", parameters, App.CreationToken);
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var application = parameters.Application;
            Database = application.Name;

            using (var s = Session)
            {
                DatabaseExists.Ensure(DocumentStore, Database);

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
                    config = new Config(parameters.AdminEmails, parameters.Roles, parameters.Application.Description, parameters.Application.AdminUrl, parameters.Application.BaseUrl);

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

                    var adminUser = new User("admin", "user", useremail, "", hashed.HashedPassword, hashed.Salt,
                                             application.Name, "admin", "admintoken", null, null)
                        {
                            Active = true,
                            Approved = true
                        };

                    s.Store(adminUser);

                    CommandExecutor.ExecuteCommand(
                        new PasswordResetEmailCommand(new PasswordResetEmailCommand.MailTemplate(
                                                          new[] {adminUser.Email},
                                                          config.AdministrativeEmails,
                                                          adminUser.FullName,
                                                          password,
                                                          parameters.Application.Description)));
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
                var errors = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));

                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    string.Format("Missing parameters. {0}", errors)));
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

                var response =
                    await
                    CommandExecutor.ExecuteCommandAsync(new AcceptUserCommandAsync(s, info, user, adminUser.FullName));

                if (response != null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, response);
                }

                return Request.CreateResponse(HttpStatusCode.Accepted);
            }
        }

        [HttpDelete]
        public async Task<HttpResponseMessage> Reject(RejectRequestInformation info)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                                         .SelectMany(x => x.Errors)
                                         .Select(x => x.ErrorMessage));

                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    string.Format("Missing parameters. {0}", errors)));
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

                await CommandExecutor.ExecuteCommandAsync(new RejectUserCommandAsync(s, user, adminUser.FullName));

                return Request.CreateResponse(HttpStatusCode.Accepted);
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetAllWaiting(string application = null, string adminToken = null)
        {
            if (!ValidationService.IsValid(application))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            if (string.IsNullOrEmpty(adminToken) || !adminToken.Contains("."))
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                              new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                    "Bad Token."));
            }

            Database = application;

            using (var s = AsyncSession)
            {
                var adminTokenParts = adminToken.Split('.');
                if (adminTokenParts.Length != 2)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        "Bad Token."));
                }

                var adminUser = await s.LoadAsync<User>(adminTokenParts[0]);
                if (adminUser.AdminToken != adminToken)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        "Bad Token."));
                }

                var waitingUsers = await s.Query<User, UsersByApprovedIndex>()
                                          .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                          .Where(x => x.Active && x.Approved == false)
                                          .ToListAsync();

                return Request.CreateResponse(HttpStatusCode.OK,
                                              new ResponseContainer<IList<User>>(waitingUsers));
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetAllApproved(string application = null, string adminToken = null)
        {
            if (!ValidationService.IsValid(application))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            if (string.IsNullOrEmpty(adminToken) || !adminToken.Contains("."))
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                              new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                    "Bad Token."));
            }

            Database = application;

            using (var s = AsyncSession)
            {
                var adminTokenParts = adminToken.Split('.');
                if (adminTokenParts.Length != 2)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        "Bad Token."));
                }

                var adminUser = await s.LoadAsync<User>(adminTokenParts[0]);
                if (adminUser.AdminToken != adminToken)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        "Bad Token."));
                }

                var waitingUsers = await s.Query<User, UsersByApprovedIndex>()
                                          .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                          .Where(x => x.Active && x.Approved)
                                          .ToListAsync();

                return Request.CreateResponse(HttpStatusCode.OK,
                                              new ResponseContainer<IList<User>>(waitingUsers));
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

        [HttpPut]
        public async Task<HttpResponseMessage> UpdateUser(UpdateUser user)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(user.UserId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ResponseContainer(HttpStatusCode.BadRequest,
                                                                    "Missing parameters."));
            }

            if (string.IsNullOrEmpty(user.AdminToken) || !user.AdminToken.Contains("."))
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                              new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                    "Bad Token."));
            }

            Database = user.Application;

            using (var s = AsyncSession)
            {
                var adminTokenParts = user.AdminToken.Split('.');
                if (adminTokenParts.Length != 2)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        "Bad Token."));
                }

                var adminUser = await s.LoadAsync<User>(adminTokenParts[0]);
                if (adminUser.AdminToken != user.AdminToken)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized,
                                                  new ResponseContainer(HttpStatusCode.Unauthorized,
                                                                        "Bad Token."));
                }

                var dbuser = await s.LoadAsync<User>(user.UserId);

                if (dbuser == null)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                                                  new ResponseContainer(HttpStatusCode.PreconditionFailed,
                                                                        "User not found."));
                }

                dbuser.Email = user.Email;
                dbuser.Role = user.Role;
                dbuser.First = user.First;
                dbuser.Last = user.Last;
                dbuser.Agency = user.Agency;
                dbuser.AccessRules = user.AccessRules;

                if (user.Additional != null)
                {
                    dbuser.AdditionalSerialized = JsonConvert.SerializeObject(user.Additional);
                }

                await s.SaveChangesAsync();
            }

            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
    }

}