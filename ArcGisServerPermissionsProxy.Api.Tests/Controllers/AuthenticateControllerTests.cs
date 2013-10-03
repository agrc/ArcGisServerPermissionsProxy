using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using AgrcPasswordManagement.Commands;
using AgrcPasswordManagement.Models.Account;
using ArcGisServerPermissionsProxy.Api.Controllers;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Models.Response.Authentication;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using ArcGisServerPermissionsProxy.Api.Services.Token;
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

            var user = new User("USERNAME", "test@test.com", "AGENCY", password.HashedPassword, salt, null,
                                "admin");
            var app = new Application(null, "test");

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

        [Test]
        public async Task UserCanAuthenticatewithCorrectPassword()
        {
            var login = new LoginCredentials("test@test.com", "123abc", null);

            var response = await _controller.UserLogin(login);

            var result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int) HttpStatusCode.OK));
            Assert.That(result.Result, Is.Not.Null);
            Assert.That(result.Result.Token, Is.Not.Null);
        }

        [Test]
        public async Task UserIsDeniedOnBadPassword()
        {
            var login = new LoginCredentials("test@test.com", "wrong", null);

            var response = await _controller.UserLogin(login);

            var result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int) HttpStatusCode.Unauthorized));
            Assert.That(result.Result, Is.Null);
        }
    }
}