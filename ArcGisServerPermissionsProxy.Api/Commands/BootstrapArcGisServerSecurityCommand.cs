using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ArcGisServerPermissionProxy.Domain;
using ArcGisServerPermissionProxy.Domain.ArcGIS;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using CommandPattern;
using NLog;

namespace ArcGisServerPermissionsProxy.Api.Commands
{
    public class BootstrapArcGisServerSecurityCommandAsync : CommandAsync<IEnumerable<string>> 
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _urlTemplate = "http{0}://{1}{2}/{3}/";
        private readonly string _createUserUrl = "{0}admin/security/users/add";
        private readonly string _createRoleUrl = "{0}admin/security/roles/add";
        private readonly string _assignRoleUrl = "{0}admin/security/users/assignRoles";

        public AdminCredentials AdminInformation;
        public CreateApplicationParams Parameters;

        public BootstrapArcGisServerSecurityCommandAsync()
        {
            var port = App.Port > 0 ? ":" + App.Port.ToString(CultureInfo.InvariantCulture) : "";
            _urlTemplate = string.Format(_urlTemplate, App.Ssl ? "s" : "", App.ArcGisHostUrl, port, App.Instance);
            _createUserUrl = string.Format(_createUserUrl, _urlTemplate);
            _createRoleUrl = string.Format(_createRoleUrl, _urlTemplate);
            _assignRoleUrl = string.Format(_assignRoleUrl, _urlTemplate);

            Logger.Warn("Create User: {0}, Role: {1}, Assign: {2}", _createUserUrl, _createRoleUrl, _assignRoleUrl);
        }

        public BootstrapArcGisServerSecurityCommandAsync(CreateApplicationParams parameters,
                                                         AdminCredentials adminInformation) : this()
        {
            Parameters = parameters;
            AdminInformation = adminInformation;
        }

        public override async Task<IEnumerable<string>> Execute()
        {
            var responseMessages = new Collection<string>();

            using (var client = new HttpClient())
            {
                var token =
                    await
                    CommandExecutor.ExecuteCommandAsync(
                        new GetTokenCommandAsync(new GetTokenCommandAsyncBase.GetTokenParams(App.ArcGisHostUrl,
                          App.Instance, App.Ssl, App.Port), new GetTokenCommandAsyncBase.User(AdminInformation.Username,
                                                                                   AdminInformation.Password)));

                //post to create user
                var usersAndRolesToCreate =
                    Parameters.Roles.Select(x => string.Format("{0}_{1}", Parameters.Application.Name, x));

                var mediaType = new[]{new TextPlainResponseFormatter()};

                foreach (var name in usersAndRolesToCreate)
                {
                    Logger.Info("Posting to create user: {0}, {1}, {2}, {3}", _createUserUrl, name, ConfigurationManager.AppSettings["accountPassword"], token.Token);
                    var createUserRequest = await client.PostAsync(_createUserUrl,
                                                                    new FormUrlEncodedContent(new Dictionary
                                                                                                  <string, string>
                                                                        {
                                                                            {"username", name},
                                                                            {"password", ConfigurationManager.AppSettings["accountPassword"]},
                                                                            {"f", "json"},
                                                                            {"token", token.Token}
                                                                        }));

                    Logger.Info(createUserRequest.IsSuccessStatusCode);
                    var response = await createUserRequest.Content.ReadAsStringAsync();
                    Debug.Print(response);
                    Logger.Info("Create user response: {0}", response);

                    var createUserResponse = await createUserRequest.Content.ReadAsAsync<AdminServerStatus>(mediaType);

                    foreach (var message in createUserResponse.Messages)
                    {
                        responseMessages.Add(message);
                    }

                    var createRoleRequest = await client.PostAsync(_createRoleUrl,
                                                                    new FormUrlEncodedContent(new Dictionary
                                                                                                  <string, string>
                                                                        {
                                                                            {"rolename", name},
                                                                            {"f", "json"},
                                                                            {"token", token.Token}
                                                                        }));

                    var createRoleResponse = await createRoleRequest.Content.ReadAsAsync<AdminServerStatus>(mediaType);

                    foreach (var message in createRoleResponse.Messages)
                    {
                        responseMessages.Add(message);
                    }

                    response = await createRoleRequest.Content.ReadAsStringAsync();
                    Debug.Print(response);
                    Debug.Print(response);
                    Logger.Info(response);

                    var assignRoleRequest = await client.PostAsync(_assignRoleUrl,
                                                                    new FormUrlEncodedContent(new Dictionary
                                                                                                  <string, string>
                                                                        {
                                                                            {"username", name},
                                                                            {"roles", name},
                                                                            {"f", "json"},
                                                                            {"token", token.Token}
                                                                        }));

                    var assignRoleResponse = await assignRoleRequest.Content.ReadAsAsync<AdminServerStatus>(mediaType);

                    foreach (var message in assignRoleResponse.Messages)
                    {
                        responseMessages.Add(message);
                    }

                    response = await assignRoleRequest.Content.ReadAsStringAsync();
                    Debug.Print(response);
                    Debug.Print(response);
                    Logger.Info(response);
                }
            }

            return responseMessages;
        }

        public override string ToString()
        {
            return string.Format("{0}, Parameters: {1}", "BootstrapArcGisServerSecurityCommand", Parameters);
        }
    }
}