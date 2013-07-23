using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionsProxy.Api.Controllers.Admin;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using ArcGisServerPermissionsProxy.Api.Tests.Infrastructure;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Controllers
{
    public class AdminControllerTests : RavenEmbeddableTest
    {
        private const string Database = "";
        private AdminController _controller;

        public override void SetUp()
        {
            base.SetUp();

            var appConfig = new Config(new[] { "admin1@email.com", "admin2@email.com" });

            var hashedPassword =
                CommandExecutor.ExecuteCommand(new HashPasswordCommand("password", "SALT", ")(*&(*^%*&^$*^#$"));


            var notApprovedActiveUser = new User("Not Approved but Active", "notApprovedActiveUser@test.com", "AGeNCY", hashedPassword.Result.HashedPassword, "SALT", null,
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

            _controller = new AdminController
            {
                Request = request,
                DocumentStore = DocumentStore
            };

            _controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
        }

        [Test]
        public async Task AcceptUserSetsTheUserAcceptPropertyToTrue()
        {
            var response = await
                           _controller.Accept(new AdminController.AcceptRequestInformation("notApprovedActiveUser@test.com",
                                                                                          "Monkey", "emptyToken", Database));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "notApprovedActiveUser@test.com".ToLowerInvariant());

                Assert.That(user.Approved, Is.True);
                Assert.That(user.Role, Is.EquivalentTo("monkey"));
            }
        }

        [Test]
        public async Task AcceptUserFailsGracefully()
        {
            var response = await
                           _controller.Accept(new AdminController.AcceptRequestInformation("where@am.i",
                                                                                         "Monkey", "emptyToken", Database));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task RejectUserRemovesAllPrivs()
        {
            var response = await
                           _controller.Reject(new AdminController.RejectRequestInformation("approvedActiveUser@test.com", "emptyToken", Database));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "approvedActiveUser@test.com".ToLowerInvariant());

                Assert.That(user.Approved, Is.False);
                Assert.That(user.Active, Is.False);
                Assert.That(user.Role, Is.Empty);
            }
        }

    }
}