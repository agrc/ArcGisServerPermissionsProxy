using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Controllers.Admin;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using CommandPattern;

namespace ArcGisServerPermissionsProxy.Api.Commands
{
    public class BootstrapArcGisServerSecurityCommandAsync : CommandAsync
    {
        private const string CreateUserUrl = "http://localhost/arcgis/admin/security/users/add";
        private const string CreateRoleUrl = "http://localhost/arcgis/admin/security/roles/add";
        private const string AssignRoleUrl = "http://localhost/arcgis/admin/security/users/assignRoles";

        private readonly AdminController.CreateApplicationParams _parameters;
        private readonly AdminCredentials _adminInformation;

        public BootstrapArcGisServerSecurityCommandAsync(AdminController.CreateApplicationParams parameters, AdminCredentials adminInformation)
        {
            _parameters = parameters;
            _adminInformation = adminInformation;
        }

        public override async Task<bool> Execute()
        {
            using (var client = new HttpClient())
            {
                var token =
                    await
                    CommandExecutor.ExecuteCommandAsync(
                        new GetTokenCommandAsync(new GetTokenCommandAsyncBase.GetTokenParams(),
                                                 new GetTokenCommandAsyncBase.User(_adminInformation.Username, _adminInformation.Password)));

                //post to create user
                var usersAndRolesToCreate =
                    _parameters.Roles.Select(x => string.Format("{0}_{1}", _parameters.Application, x));

                foreach (var name in usersAndRolesToCreate)
                {
                    var createUserResponse =await  client.PostAsync(CreateUserUrl,
                                                              new FormUrlEncodedContent(new Dictionary<string, string>
                                                                  {
                                                                      {"username", name},
                                                                      {"password", "test"},
                                                                      {"f", "json"},
                                                                      {"token", token.Token}
                                                                  }));

                    Debug.Print(await createUserResponse.Content.ReadAsStringAsync());

                    var createRoleResponse = await client.PostAsync(CreateRoleUrl,
                                                              new FormUrlEncodedContent(new Dictionary<string, string>
                                                                  {
                                                                      {"rolename", name},
                                                                      {"f", "json"},
                                                                      {"token", token.Token}
                                                                  }));

                    Debug.Print(await createRoleResponse.Content.ReadAsStringAsync());

                    var assignRoleResponse = await client.PostAsync(AssignRoleUrl,
                                                              new FormUrlEncodedContent(new Dictionary<string, string>
                                                                  {
                                                                      {"username", name},
                                                                      {"roles", name},
                                                                      {"f", "json"},
                                                                      {"token", token.Token}
                                                                  }));

                    Debug.Print(await assignRoleResponse.Content.ReadAsStringAsync());
                }
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("{0}, Parameters: {1}", "BootstrapArcGisServerSecurityCommand", _parameters);
        }
    }
}