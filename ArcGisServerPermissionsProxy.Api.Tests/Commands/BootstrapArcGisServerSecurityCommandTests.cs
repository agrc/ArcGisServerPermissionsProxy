using System.Configuration;
using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Controllers.Admin;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Commands
{
    [TestFixture]
    public class BootstrapArcGisServerSecurityCommandTests
    {
        private AdminCredentials _adminCredentials;

        [SetUp]
        public void Setup()
        {
            _adminCredentials = new AdminCredentials(ConfigurationManager.AppSettings["adminUserName"],
                                                     ConfigurationManager.AppSettings["adminPassword"]);
        }

        [Test, Explicit]
        public async Task CreatesUsersRolesAndAssignsUsersToRoles()
        {
            var command = new BootstrapArcGisServerSecurityCommandAsync(new AdminController.CreateApplicationParams
                {
                    Application = "unitTests",
                    Roles = new[] {"admin", "publisher", "editor", "readonly"}
                }, _adminCredentials);

            await CommandExecutor.ExecuteCommandAsync(command);
        }
    }
}