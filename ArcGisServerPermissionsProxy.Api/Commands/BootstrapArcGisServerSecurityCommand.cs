using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ArcGisServerPermissionProxy.Domain;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using CommandPattern;
using NLog;

namespace ArcGisServerPermissionsProxy.Api.Commands
{
    public class BootstrapArcGisServerSecurityCommandAsync : CommandAsync<IEnumerable<string>> 
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _createUserUrl = "http://{0}/arcgis/admin/security/users/add";
        private readonly string _createRoleUrl = "http://{0}/arcgis/admin/security/roles/add";
        private readonly string _assignRoleUrl = "http://{0}/arcgis/admin/security/users/assignRoles";

        public AdminCredentials AdminInformation;
        public CreateApplicationParams Parameters;

        public BootstrapArcGisServerSecurityCommandAsync()
        {
            _createUserUrl = string.Format(_createUserUrl, App.ArcGisHostUrl);
            _createRoleUrl = string.Format(_createRoleUrl, App.ArcGisHostUrl);
            _assignRoleUrl = string.Format(_assignRoleUrl, App.ArcGisHostUrl);

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
                    var createUserRequest = await client.PostAsync(_createUserUrl,
                                                                    new FormUrlEncodedContent(new Dictionary
                                                                                                  <string, string>
                                                                        {
                                                                            {"username", name},
                                                                            {"password", ConfigurationManager.AppSettings["accountPassword"]},
                                                                            {"f", "json"},
                                                                            {"token", token.Token}
                                                                        }));

                    var createUserResponse = await createUserRequest.Content.ReadAsAsync<AdminServerStatus>(mediaType);

                    foreach (var message in createUserResponse.Messages)
                    {
                        responseMessages.Add(message);
                    }

                    var response = await createUserRequest.Content.ReadAsStringAsync();
                    Debug.Print(response);
                    Logger.Info(response);

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