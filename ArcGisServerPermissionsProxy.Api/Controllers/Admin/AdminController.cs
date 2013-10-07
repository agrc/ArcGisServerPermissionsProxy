using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Commands.Query;
using ArcGisServerPermissionsProxy.Api.Commands.Users;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using CommandPattern;
using Raven.Client.Extensions;
using Raven.Client.Indexes;

namespace ArcGisServerPermissionsProxy.Api.Controllers.Admin
{
    public class AdminController : RavenApiController
    {
        [HttpPost]
        public async Task<HttpResponseMessage> CreateApplication(CreateApplicationParams parameters)
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

                IndexCreation.CreateIndexes(provider, DocumentStore.DatabaseCommands.ForDatabase(Database),
                                            DocumentStore.Conventions);

                if (!parameters.Roles.Select(x => x.ToLower()).Contains("admin"))
                {
                    parameters.Roles.Add("admin");
                }

                var existingConfig = s.Load<Config>("1");
                if (existingConfig == null)
                {
                    var config = new Config(parameters.AdminEmails, parameters.Roles);

                    s.Store(config, "1");
                    s.SaveChanges();
                }
            }

            //add admin email to admin group and send email to reset password.
            var messages =
                await
                CommandExecutor.ExecuteCommandAsync(new BootstrapArcGisServerSecurityCommandAsync(parameters,
                                                                                                  App.AdminInformation));

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

            Database = info.Application;

            using (var s = AsyncSession)
            {
                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserCommandAsync(info.Email, s));

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

                var info = new AcceptRequestInformation(user.Email, role, token, application);

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

            Database = info.Application;

            using (var s = AsyncSession)
            {
                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserCommandAsync(info.Email, s));

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

        /// <summary>
        ///     A class for accepting users in the application
        /// </summary>
        public class AcceptRequestInformation : UserController.RequestInformation
        {
            public AcceptRequestInformation()
            {
            }

            public AcceptRequestInformation(string email, string role, Guid token, string application)
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

        public class CreateApplicationParams
        {
            [Required]
            public string Application { get; set; }

            [Required]
            public string[] AdminEmails { get; set; }

            [Required]
            public Collection<string> Roles { get; set; }
        }

        /// <summary>
        ///     A class for getting user role requests
        /// </summary>
        public class RejectRequestInformation : UserController.RequestInformation
        {
            public RejectRequestInformation(string email, Guid token, string application)
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