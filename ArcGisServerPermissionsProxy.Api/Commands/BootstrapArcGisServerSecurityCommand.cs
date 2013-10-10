using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Controllers.Admin;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using CommandPattern;

namespace ArcGisServerPermissionsProxy.Api.Commands
{
    public class BootstrapArcGisServerSecurityCommandAsync : CommandAsync<IEnumerable<string>> 
    {
        private const string CreateUserUrl = "http://localhost/arcgis/admin/security/users/add";
        private const string CreateRoleUrl = "http://localhost/arcgis/admin/security/roles/add";
        private const string AssignRoleUrl = "http://localhost/arcgis/admin/security/users/assignRoles";

        public AdminCredentials AdminInformation;
        public AdminController.CreateApplicationParams Parameters;

        public BootstrapArcGisServerSecurityCommandAsync()
        {
            
        }

        public BootstrapArcGisServerSecurityCommandAsync(AdminController.CreateApplicationParams parameters,
                                                         AdminCredentials adminInformation)
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
                        new GetTokenCommandAsync(new GetTokenCommandAsyncBase.GetTokenParams(),
                                                 new GetTokenCommandAsyncBase.User(AdminInformation.Username,
                                                                                   AdminInformation.Password)));

                //post to create user
                var usersAndRolesToCreate =
                    Parameters.Roles.Select(x => string.Format("{0}_{1}", Parameters.Application, x));

                var mediaType = new[]{new TextPlainResponseFormatter()};

                foreach (var name in usersAndRolesToCreate)
                {
                    var createUserRequest = await client.PostAsync(CreateUserUrl,
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

                    Debug.Print(await createUserRequest.Content.ReadAsStringAsync());

                    var createRoleRequest = await client.PostAsync(CreateRoleUrl,
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

                    Debug.Print(await createRoleRequest.Content.ReadAsStringAsync());

                    var assignRoleRequest = await client.PostAsync(AssignRoleUrl,
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


                    Debug.Print(await assignRoleRequest.Content.ReadAsStringAsync());
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