using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionProxy.Domain;
using ArcGisServerPermissionProxy.Domain.Account;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Controllers;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Services;
using ArcGisServerPermissionsProxy.Api.Tests.Infrastructure;
using CommandPattern;
using NUnit.Framework;
using Raven.Imports.Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api.Tests.Controllers
{
    public class UserControllerTests : RavenEmbeddableTest
    {
        private const string Database = "";
        private UserController _controller;

        public override void SetUp()
        {
            base.SetUp();

            var appConfig = new Config(new[] {"admin1@email.com", "admin2@email.com"}, new[] {"admin", "role2", "role3", "role4"}, "unit test description", "http://testurl.com/admin.html");

            var hashedPassword =
                CommandExecutor.ExecuteCommand(new HashPasswordCommand("password", "SALT", ")(*&(*^%*&^$*^#$"));


            var notApprovedActiveUser = new User("Not Approved","but Active", "notApprovedActiveUser@test.com", "AGENCY",
                                                 hashedPassword.Result.HashedPassword, "SALT", null,
                                                 null, null, null);

            var approvedActiveUser = new User("Approved","and Active", "approvedActiveUser@test.com", "AGENCY",
                                              hashedPassword.Result.HashedPassword, "SALT", null,
                                              "admin", null, null)
                {
                    Active = false,
                    Approved = true
                };

            var notApprovedNotActiveUser = new User("Not approved","or active", "notApprovedNotActiveUser@test.com",
                                                    "AGENCY", hashedPassword.Result.HashedPassword, "SALT", null,
                                                    null, null, null)
                {
                    Active = false
                };

            using (var s = DocumentStore.OpenSession())
            {
                s.Store(appConfig, "1");
                s.Store(approvedActiveUser);
                s.Store(notApprovedActiveUser);
                s.Store(notApprovedNotActiveUser);

                s.SaveChanges();
            }

            var config = new HttpConfiguration();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/");

            _controller = new UserController
                {
                    Request = request,
                    DocumentStore = DocumentStore,
                    UrlBuilder = new MockUrlBuilder()
                };

            _controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
        }

        [Test]
        public async Task ResetPasswordChangesSaltAndPassword()
        {
            var response = await
                           _controller.ResetPassword(
                               new ResetRequestInformation("approvedActiveUser@test.com", Database,
                                                                          Guid.Empty));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "approvedActiveUser@test.com".ToLowerInvariant());

                Assert.That(user.Password, Is.Not.EqualTo("password"));
                Assert.That(user.Salt, Is.Not.EqualTo(""));
            }
        }

        [Test]
        public async Task ResetPasswordFailsGracefullyIfUserDoesntExist()
        {
            var response = await
                           _controller.ResetPassword(
                               new ResetRequestInformation("notauser@test.com", Database,
                                                                          Guid.Empty));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
        }

        [Test]
        public async Task ChangePasswordChangesTheUsersPassword()
        {
            var response =
                await
                _controller.ChangePassword(
                    new ChangePasswordRequestInformation("approvedActiveUser@test.com", "password",
                                                                        "newPassword", "newPassword", "", Guid.Empty));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var hashedPassword =
                CommandExecutor.ExecuteCommand(new HashPasswordCommand("newPassword", "SALT", ")(*&(*^%*&^$*^#$"));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "approvedActiveUser@test.com");


                Assert.That(user.Password, Is.EqualTo(hashedPassword.Result.HashedPassword));
            }
        }

        [Test]
        public async Task ChangePasswordFailsGracefullyIfCurrentPasswordIsWrong()
        {
            var response =
                await
                _controller.ChangePassword(
                    new ChangePasswordRequestInformation("approvedActiveUser@test.com", "wrong",
                                                                        "newPassword", "newPassword", "", Guid.Empty));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
        }

        [Test]
        public async Task ChangePasswordFailsGracefullyIfPasswordsDontMatch()
        {
            var response =
                await
                _controller.ChangePassword(
                    new ChangePasswordRequestInformation("approvedActiveUser@test.com", "password", "1",
                                                                        "2", "", Guid.Empty));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
        }

        [Test]
        public async Task CanRegisterWithAdditionalOptions()
        {
            var additional = new {address = "123 house st", phone = "111"};
            var response = await _controller.Register(new Credentials("additional@options.com", "aA123456!", null)
                {
                    Additional = additional,
                    Agency = "Agency",
                    First = "First",
                    Last = "Last"
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResults())
                            .Single(x => x.Email == "additional@options.com");

                var temp = JsonConvert.SerializeObject(additional);
                var expected = JsonConvert.DeserializeObject(temp);

                Assert.That(user.Additional, Is.EqualTo(expected));
            }
        }
    }
}