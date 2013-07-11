using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using AgrcPasswordManagement.Commands;
using AgrcPasswordManagement.Models.Account;
using ArcGisServerPermissionsProxy.Api.Controllers;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Models.Response.Authentication;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using ArcGisServerPermissionsProxy.Api.Services;
using ArcGisServerPermissionsProxy.Api.Tests.Infrastructure;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Controllers
{
    [TestFixture]
    public class AuthenticateControllerTests : RavenEmbeddableTest
    {
        public override void SetUp()
        {
            base.SetUp();

            var salt = CommandExecutor.ExecuteCommand(new GenerateSaltCommand());
            var password = CommandExecutor.ExecuteCommand(new HashPasswordCommand("123abc", salt, Pepper)).Result;

            var user = new User("USERNAME", "test@test.com", "AGENCY", password.HashedPassword, salt, "",
                                new Collection<string> {"admin"});
            var app = new Application("", "test");

            using (var s = DocumentStore.OpenSession())
            {
                if (!s.Query<User, UserByEmailIndex>()
                      .Any(x => x.Email == user.Email))
                {
                    s.Store(user);
                }

                if (!s.Query<Application, ApplicationByNameIndex>()
                      .Any(x => x.Name == app.Name))
                {
                    s.Store(app, app.Name);
                }

                s.SaveChanges();
            }

            var config = new HttpConfiguration();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/");

            _controller = new AuthenticateController
                {
                    Request = request,
                    DocumentStore = DocumentStore,
                    TokenService = new MockTokenService()
                };

            _controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
        }

        private AuthenticateController _controller;
        private const string Pepper = ")(*&(*^%*&^$*^#$";

        private static ResponseContainer<AuthenticationResponse> GetResultContent(HttpResponseMessage response)
        {
            //Debug.Print(response.Result.Content.ReadAsStringAsync().Result);

            return response.Content.ReadAsAsync<ResponseContainer<AuthenticationResponse>>(new[]
                {
                    new TextPlainResponseFormatter()
                }).Result;
        }

        [Test, Explicit]
        public void SeedingWorks()
        {
            using (var s = DocumentStore.OpenSession())
            {
                var users = s.Query<User, UserByEmailIndex>()
                             .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                             .Customize(x => x.Include<Application>(o => o.Name))
                             .Where(x => x.Email == "test@test.com");

                Assert.That(users.Count(), Is.EqualTo(1));

                var app = s.Load<Application>(users.First().Application);

                Assert.That(app.Name, Is.Null);

                var apps = s.Query<Application, ApplicationByNameIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Count();

                Assert.That(apps, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task UserCanAuthenticatewithCorrectPassword()
        {
            var login = new LoginCredentials("test@test.com", "123abc", "admin", "");

            var response = await _controller.User(login);

            var result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int) HttpStatusCode.OK));
            Assert.That(result.Result, Is.Not.Null);
            Assert.That(result.Result.Token, Is.Not.Null);
        }

        [Test]
        public async Task UserIsDeniedOnBadPassword()
        {
            var login = new LoginCredentials("test@test.com", "wrong", "admin", "");

            var response = await _controller.User(login);

            var result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int) HttpStatusCode.Unauthorized));
            Assert.That(result.Result, Is.Null);
        }
    }
}