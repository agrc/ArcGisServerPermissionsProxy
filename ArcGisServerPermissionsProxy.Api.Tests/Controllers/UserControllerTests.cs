using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionsProxy.Api.Controllers;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using ArcGisServerPermissionsProxy.Api.Services;
using ArcGisServerPermissionsProxy.Api.Tests.Infrastructure;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Controllers
{
    public class UserControllerTests : RavenEmbeddableTest
    {
        private const string Database = "";
        private UserController _controller;

        public override void SetUp()
        {
            base.SetUp();

            var appConfig = new Config(new[] {"admin1@email.com", "admin2@email.com"});

            var hashedPassword =
                CommandExecutor.ExecuteCommand(new HashPasswordCommand("password", "SALT", ")(*&(*^%*&^$*^#$"));


            var notApprovedActiveUser = new User("Not Approved but Active", "notApprovedActiveUser@test.com", "AGENCY", hashedPassword.Result.HashedPassword, "SALT", null,
                                                 null);

            var approvedActiveUser = new User("Approved and Active", "approvedActiveUser@test.com", "AGENCY", hashedPassword.Result.HashedPassword, "SALT", null,
                                              "admin")
                {
                    Active = false,
                    Approved = true
                };

            var notApprovedNotActiveUser = new User("Not approved or active", "notApprovedNotActiveUser@test.com", "AGENCY", hashedPassword.Result.HashedPassword, "SALT", null,
                                                    null)
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
                    ValidationService = new MockValidationService()
                };

            _controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
        }

        [Test]
        public async Task GetAllWaitingReturnsAllActiveNotApprovedUsers()
        {
            var response = await _controller.GetAllWaiting(Database);

            var result = await response.Content.ReadAsAsync<ResponseContainer<IList<User>>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetRolesGetsTheRolesForSpecificUser()
        {
            var response = await _controller.GetRoles("approvedActiveUser@test.com", Database);

            var result = await response.Content.ReadAsAsync<ResponseContainer<string>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Result, Is.EqualTo("admin"));
        }

        [Test]
        public async Task GetRolesFailsGracefully()
        {
            var response = await _controller.GetRoles("where@am.i", Database);

            var result = await response.Content.ReadAsAsync<ResponseContainer<IList<string>>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Status, Is.EqualTo(404));
            Assert.That(result.Message, Is.EqualTo("User not found."));
        }

        [Test]
        public async Task ResetPasswordChangesSaltAndPassword()
        {
            var response = await
                           _controller.ResetPassword(new UserController.ResetRequestInformation("approvedActiveUser@test.com", Database, "emptyToken"));

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
        public async Task ChangePasswordChangesTheUsersPassword()
        {
            var response = await _controller.ChangePassword(new UserController.ChangePasswordRequestInformation("approvedActiveUser@test.com", "password", "newPassword", "newPassword", "", ""));

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
            var response = await _controller.ChangePassword(new UserController.ChangePasswordRequestInformation("approvedActiveUser@test.com", "wrong", "newPassword", "newPassword", "", ""));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
        }

        [Test]
        public async Task ChangePasswordFailsGracefullyIfPasswordsDontMatch()
        {
            var response = await _controller.ChangePassword(new UserController.ChangePasswordRequestInformation("approvedActiveUser@test.com", "password", "1", "2", "", ""));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
        }
    }
}