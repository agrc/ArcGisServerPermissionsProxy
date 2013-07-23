using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Commands.Query;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using ArcGisServerPermissionsProxy.Api.Services.Token;
using CommandPattern;
using Ninject;
using Raven.Client.Extensions;
using Raven.Client.Indexes;

namespace ArcGisServerPermissionsProxy.Api.Controllers.Admin
{
    public class AdminController : RavenApiController
    {
        [HttpPost]
        public HttpResponseMessage CreateApplication(CreateApplicationParams parameters)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            var database = parameters.Application;
            Database = database;

            using (var s = Session)
            {
                DocumentStore.DatabaseCommands.EnsureDatabaseExists(Database);
                
                var catalog = new AssemblyCatalog(typeof (UserByEmailIndex).Assembly);
                var provider = new CatalogExportProvider(catalog)
                    {
                        SourceProvider = new CatalogExportProvider(catalog)
                    };

                IndexCreation.CreateIndexes(provider, DocumentStore.DatabaseCommands.ForDatabase(Database), DocumentStore.Conventions);

                var existingConfig = s.Load<Config>("1");
                if (existingConfig == null)
                {
                    var config = new Config(parameters.AdminEmails, parameters.Roles);

                    s.Store(config, "1");
                    s.SaveChanges();
                }
            }

            CommandExecutor.ExecuteCommandAsync(new BootstrapArcGisServerSecurityCommandAsync(parameters, App.AdminInformation));

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
                var config = await s.LoadAsync<Config>("1");

                //get role make sure exists
                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserCommandAsync(info.Email, s));

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new ResponseContainer(HttpStatusCode.NotFound, "User was not found."));
                }

                if (!config.Roles.Contains(info.Role.ToLowerInvariant()))
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new ResponseContainer(HttpStatusCode.NotFound, "Role was not found.")); 
                }

                user.Active = true;
                user.Approved = true;
                user.Role = info.Role;

                await s.SaveChangesAsync();

                Task.Factory.StartNew(
                    () =>
                    CommandExecutor.ExecuteCommand(
                        new UserAcceptedEmailCommand(new UserAcceptedEmailCommand.MailTemplate(new[] { user.Email },
                                                                                               config.
                                                                                                   AdministrativeEmails,
                                                                                               user.Name, info.Role,
                                                                                               user.Email,
                                                                                               user.Application))));

                return Request.CreateResponse(HttpStatusCode.NoContent);
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

            Database = info.Application;

            using (var s = AsyncSession)
            {
                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserCommandAsync(info.Email, s));

                user.Active = false;
                user.Approved = false;
                user.Role = null;

                await s.SaveChangesAsync();

                var config = await s.LoadAsync<Config>("1");

                Task.Factory.StartNew(() =>
                                      CommandExecutor.ExecuteCommand(
                                          new UserRejectedEmailCommand(
                                              new UserRejectedEmailCommand.MailTemplate(new[] { user.Email },
                                                                                        config.AdministrativeEmails,
                                                                                        user.Name,
                                                                                        user.Application))));


                return Request.CreateResponse(HttpStatusCode.Accepted);
            }
        }

        /// <summary>
        ///     A class for accepting users in the application
        /// </summary>
        public class AcceptRequestInformation : UserController.RequestInformation
        {
            public AcceptRequestInformation(string email, string role, string token, string application)
                : base(application, token)
            {
                Email = email;
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

            /// <summary>
            ///     Gets or sets the roles.
            /// </summary>
            /// <value>
            ///     The roles.
            /// </value>
            [Required]
            public string Role { get; set; }
        }


        /// <summary>
        ///     A class for getting user role requests
        /// </summary>
        public class RejectRequestInformation : UserController.RequestInformation
        {
            public RejectRequestInformation(string email, string token, string application)
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


        public class CreateApplicationParams
        {
            [Required]
            public string Application { get; set; }

            [Required]
            public string[] AdminEmails { get; set; }

            [Required]
            public string[] Roles { get; set; }
        }
    }
}