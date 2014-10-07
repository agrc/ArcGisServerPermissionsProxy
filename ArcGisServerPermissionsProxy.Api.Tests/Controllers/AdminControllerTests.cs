using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Controllers.Admin;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Configuration;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Services;
using ArcGisServerPermissionsProxy.Api.Tests.Fakes.Raven;
using ArcGisServerPermissionsProxy.Api.Tests.Infrastructure;
using CommandPattern;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api.Tests.Controllers {

    [TestFixture]
    public class AdminControllerTests : RavenEmbeddableTest {
        public override void SetUp()
        {
            base.SetUp();
            App.Cache();

            var appConfig = new Config(new[] {"admin1@email.com", "admin2@email.com"},
                                       new[] {"admin", "role2", "role3", "role4"}, "unit test description",
                                       "http://testurl.com/admin.html");

            var hashedPassword =
                CommandExecutor.ExecuteCommand(new HashPasswordCommand("password", "SALT", ")(*&(*^%*&^$*^#$"));

            ApprovedAdmin = new User("admin", "", "admin@email.com", "AGENCY", hashedPassword.Result.HashedPassword,
                                     "SALT", null, null, "1admin.abc", null, null);

            var notApprovedActiveUser = new User("Not Approved", " but Active", "notApprovedActiveUser@test.com",
                                                 "AGENCY",
                                                 hashedPassword.Result.HashedPassword, "SALT", null,
                                                 null, null, null, null);

            var approvedActiveUser = new User("Approved and", "Active", "approvedActiveUser@test.com", "AGENCY",
                                              hashedPassword.Result.HashedPassword, "SALT", null,
                                              "admin", null, null, null)
                {
                    Active = false,
                    Approved = true
                };

            var notApprovedNotActiveUser = new User("Not approved", "or active", "notApprovedNotActiveUser@test.com",
                                                    "AGENCY", hashedPassword.Result.HashedPassword, "SALT", null,
                                                    null, null, null, null)
                {
                    Active = false
                };

            using (var s = DocumentStore.OpenSession())
            {
                s.Store(appConfig, "1");
                s.Store(ApprovedAdmin, "1admin");
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
                    DocumentStore = DocumentStore,
                    ValidationService = new MockValidationService()
                };

            _controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
        }

        public User ApprovedAdmin { get; set; }
        private const string Database = "";
        private AdminController _controller;

        [Test]
        public async Task AcceptUserFailsGracefullyWhenEmailDoesNotExist()
        {
            var response = await
                           _controller.Accept(new AcceptRequestInformation("where@am.i",
                                                                           "Monkey", Guid.Empty,
                                                                           Database, "1admin.abc"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
        }

        [Test]
        public async Task AcceptUserFailsGracefullyWhenRoleDoesNotExist()
        {
            var response = await
                           _controller.Accept(
                               new AcceptRequestInformation("notApprovedActiveUser@test.com",
                                                            "Monkey", Guid.Empty, Database, "1admin.abc"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task AcceptUserRejectsRequestsWithNoAdminToken()
        {
            var response = await
                           _controller.Accept(
                               new AcceptRequestInformation("notApprovedActiveUser@test.com",
                                                            "ADMIN", Guid.Empty, Database, null));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "notApprovedActiveUser@test.com".ToLowerInvariant());

                Assert.That(user.Approved, Is.False);
                Assert.That(user.Role, Is.Not.EquivalentTo("admin"));
            }
        }

        [Test]
        public async Task AcceptUserSetsTheUserAcceptPropertyToTrue()
        {
            var response = await
                           _controller.Accept(
                               new AcceptRequestInformation("notApprovedActiveUser@test.com",
                                                            "ADMIN", Guid.Empty, Database, "1admin.abc"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "notApprovedActiveUser@test.com".ToLowerInvariant());

                Assert.That(user.Approved, Is.True);
                Assert.That(user.Role, Is.EquivalentTo("admin"));
            }
        }

        [Test]
        public async Task AdminsCanEditUsers()
        {
            var updateUser = new UpdateUser
                {
                    AdminToken = "1admin.abc", 
                    Agency = "new agency",
                    UserId = "1admin",
                    Email = "new@email.com",
                    Role = "role2", // currently admin
                    First = "new first", 
                    Last = "new last", 
                    AccessRules = new User.UserAccessRules
                        {
                            EndDate = 1, StartDate = 0, Options = new {option = "option"},
                        },
                    Additional = new {additional = "additional"}
                };

            var response = await _controller.UpdateUser(updateUser);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

            using (var s = DocumentStore.OpenSession())
            {
                var userShouldNotExist = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResults())
                            .Count(x => x.Email == "admin@email.com".ToLowerInvariant());

                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResults())
                            .Single(x => x.Email == "new@email.com".ToLowerInvariant());

                Assert.That(user, Is.Not.Null);
                Assert.That(userShouldNotExist, Is.EqualTo(0));
                Assert.That(user.Agency, Is.EqualTo(updateUser.Agency));
                Assert.That(user.Role, Is.EqualTo(updateUser.Role));
                Assert.That(user.First, Is.EqualTo(updateUser.First));
                Assert.That(user.Last, Is.EqualTo(updateUser.Last));
                Assert.That(user.AccessRules.EndDate, Is.EqualTo(updateUser.AccessRules.EndDate));
                Assert.That(user.AccessRules.StartDate, Is.EqualTo(updateUser.AccessRules.StartDate));
                Assert.That(user.AccessRules.OptionsSerialized, Is.EqualTo(updateUser.AccessRules.OptionsSerialized));
                Assert.That(user.AdditionalSerialized, Is.EqualTo(JsonConvert.SerializeObject(updateUser.Additional)));
            }
        }

        [Test]
        public async Task UpdateFailsGracefullyIfUserIdIsWrong()
        {
            var updateUser = new UpdateUser
                {
                    AdminToken = "1admin.abc", 
                    Agency = "new agency",
                    UserId = "not found"
                };

            var response = await _controller.UpdateUser(updateUser);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
        }
        
        [Test]
        public async Task UpdateFailsGracefullyIfMissingUserId()
        {
            var updateUser = new UpdateUser
                {
                    AdminToken = "1admin.abc", 
                    Agency = "new agency"
                };

            var response = await _controller.UpdateUser(updateUser);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task CreateApplicationAddsAdminEmailsToUsersTableApproved()
        {
            var bootstrapMock = new Mock<BootstrapArcGisServerSecurityCommandAsync>();
            bootstrapMock.SetupAllProperties();
            bootstrapMock.Setup(x => x.Run()).Returns(
                () => Task<IEnumerable<string>>.Factory.StartNew(() => new Collection<string> {"user1", "user2"}));
            bootstrapMock.Setup(x => x.Execute()).Returns(
                () => Task<IEnumerable<string>>.Factory.StartNew(() => new Collection<string> {"user1", "user2"}));
            var databaseExistsMock = new Mock<IDatabaseExists>();

            var config = new HttpConfiguration();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/");

            var controller = new AdminController
                {
                    Request = request,
                    DocumentStore = DocumentStore,
                    BootstrapCommand = bootstrapMock.Object,
                    Database = "",
                    DatabaseExists = databaseExistsMock.Object,
                    IndexCreation = new IndexCreationFake()
                };

            controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;

            var response = await controller.CreateApplication(new CreateApplicationParams
                {
                    AdminEmails = new[] {"test@test.com"},
                    Application = new CreateApplicationParams.ApplicationInfo("", "The unit test project"),
                    Roles = new Collection<string> {"admin", "publisher"},
                    CreationToken = "super_admin"
                });

            Assert.That(response.IsSuccessStatusCode, Is.True);

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "test@test.com".ToLowerInvariant());

                Assert.That(user.Approved, Is.True);
                Assert.That(user.Role, Is.EquivalentTo("admin"));
            }
        }

        [Test]
        public async Task CreateApplicationCanCreateNewApplication()
        {
            var bootstrapMock = new Mock<BootstrapArcGisServerSecurityCommandAsync>();
            bootstrapMock.SetupAllProperties();
            bootstrapMock.Setup(x => x.Run()).Returns(
                () => Task<IEnumerable<string>>.Factory.StartNew(() => new Collection<string> {"user1", "user2"}));
            bootstrapMock.Setup(x => x.Execute()).Returns(
                () => Task<IEnumerable<string>>.Factory.StartNew(() => new Collection<string> {"user1", "user2"}));
            var databaseExistsMock = new Mock<IDatabaseExists>();

            var config = new HttpConfiguration();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/");

            var controller = new AdminController
                {
                    Request = request,
                    DocumentStore = DocumentStore,
                    BootstrapCommand = bootstrapMock.Object,
                    Database = "",
                    DatabaseExists = databaseExistsMock.Object,
                    IndexCreation = new IndexCreationFake()
                };

            controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;

            var response = await controller.CreateApplication(new CreateApplicationParams
                {
                    AdminEmails = new[] {"test@test.com"},
                    Application = new CreateApplicationParams.ApplicationInfo("", "The unit test project"),
                    Roles = new Collection<string> {"admin", "publisher"},
                    CreationToken = "super_admin"
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }

        [Test]
        public async Task CreateApplicationRequiresSecretToken()
        {
            var bootstrapMock = new Mock<BootstrapArcGisServerSecurityCommandAsync>();
            bootstrapMock.SetupAllProperties();
            bootstrapMock.Setup(x => x.Run()).Returns(
                () => Task<IEnumerable<string>>.Factory.StartNew(() => new Collection<string> {"user1", "user2"}));
            bootstrapMock.Setup(x => x.Execute()).Returns(
                () => Task<IEnumerable<string>>.Factory.StartNew(() => new Collection<string> {"user1", "user2"}));
            var databaseExistsMock = new Mock<IDatabaseExists>();

            var config = new HttpConfiguration();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/");

            var controller = new AdminController
                {
                    Request = request,
                    DocumentStore = DocumentStore,
                    BootstrapCommand = bootstrapMock.Object,
                    Database = "",
                    DatabaseExists = databaseExistsMock.Object,
                    IndexCreation = new IndexCreationFake()
                };

            controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;

            var response = await controller.CreateApplication(new CreateApplicationParams
                {
                    AdminEmails = new[] {"test@test.com"},
                    Application = new CreateApplicationParams.ApplicationInfo("", "The unit test project"),
                    Roles = new Collection<string> {"admin", "publisher"}
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task GetAllWaitingReturnsAllActiveNotApprovedUsers()
        {
            var response = await _controller.GetAllWaiting("", ApprovedAdmin.AdminToken);

            var result = await response.Content.ReadAsAsync<ResponseContainer<IList<User>>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Result.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetRoleFailsGracefully()
        {
            var response = await _controller.GetRole("where@am.i", Database);

            var result = await response.Content.ReadAsAsync<ResponseContainer<IList<string>>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Status, Is.EqualTo(404));
            Assert.That(result.Message, Is.EqualTo("User not found."));
        }

        [Test]
        public async Task GetRoleGetsTheRolesForSpecificUser()
        {
            var response = await _controller.GetRole("approvedActiveUser@test.com", Database);

            var result = await response.Content.ReadAsAsync<ResponseContainer<string>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Result, Is.EqualTo("admin"));
        }

        [Test]
        public async Task GetRolesReturnsAllRoles()
        {
            var response = await _controller.GetRoles(Database);

            var result = await response.Content.ReadAsAsync<ResponseContainer<IList<string>>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Status, Is.EqualTo(200));
            Assert.That(result.Result.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task RejectUserFailsGracefullyWhenEmailDoesNotExist()
        {
            var response = await
                           _controller.Reject(new RejectRequestInformation("where@am.i", Guid.Empty,
                                                                           Database, "1admin.abc"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PreconditionFailed));
        }

        [Test]
        public async Task RejectUserRemovesAllPrivs()
        {
            var response = await
                           _controller.Reject(new RejectRequestInformation(
                                                  "approvedActiveUser@test.com", Guid.Empty, Database, "1admin.abc"));

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