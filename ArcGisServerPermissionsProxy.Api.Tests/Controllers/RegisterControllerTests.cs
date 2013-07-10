using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using AgrcPasswordManagement.Commands;
using ArcGisServerPermissionsProxy.Api.Controllers;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Models.Response.Authentication;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using ArcGisServerPermissionsProxy.Api.Tests.Infrastructure;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Controllers
{
    public class RegisterControllerTests : RavenEmbeddableTest
    {
        private const string Pepper = ")(*&(*^%*&^$*^#$";
        private RegisterController _controller;

        public override void SetUp()
        {
            base.SetUp();

            var salt = CommandExecutor.ExecuteCommand(new GenerateSaltCommand());
            var password = CommandExecutor.ExecuteCommand(new HashPasswordCommand("123abc", salt, Pepper)).Result;

            var user = new User("test@test.com", password.HashedPassword, salt, "security_role_1", new Collection<string> { "admin" });
            var app = new Application("security_role_1", "test");

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

            _controller = new RegisterController
                {
                    Request = request,
                    DocumentStore = DocumentStore
                };

            _controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
        }

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
                             .Where(x => x.Email == "test@test.com" && x.Application == "app1");

                Assert.That(users.Count(), Is.EqualTo(1));

                var app = s.Load<Application>(users.First().Application);

                Assert.That(app.Name, Is.EqualTo("app1"));

                var apps = s.Query<Application, ApplicationByNameIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Count();

                Assert.That(apps, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task ErrorWhenDatabaseDoesNotExist()
        {
            var response =
                await
                _controller.CreateNewUser(new Credentials("email@address.com", "password", "application", "role",
                                                          "database"));

            Assert.That(response.Content.ReadAsStringAsync().Result, Is.EqualTo("\"12\""));
        }
    }
}