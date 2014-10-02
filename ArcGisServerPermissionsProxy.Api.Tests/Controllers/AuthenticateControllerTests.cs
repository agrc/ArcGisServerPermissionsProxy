using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Security;
using AgrcPasswordManagement.Commands;
using AgrcPasswordManagement.Models.Account;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionProxy.Domain.Response.Authentication;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Controllers;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
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

            var adminUser = new User("USER","NAME", "test@test.com", "AGENCY", password.HashedPassword, salt, null,
                                "admin", "adminToken");
            var normalUser = new User("USER", "", "notadmin@test.com", "AGENCY", password.HashedPassword, salt, null,
                               "publisher", null);
            var expiredUser = new User("USER", "", "time@expired.com", "AGENCY", password.HashedPassword, salt, null,
                               "publisher", null);

            var app = new Application(null, "test");

            using (var s = DocumentStore.OpenSession())
            {
                if (!s.Query<User, UserByEmailIndex>()
                      .Any(x => x.Email == adminUser.Email))
                {
                    s.Store(adminUser);
                }

                if (!s.Query<User, UserByEmailIndex>()
                     .Any(x => x.Email == normalUser.Email))
                {
                    s.Store(normalUser);
                }

                if (!s.Query<User, UserByEmailIndex>()
                     .Any(x => x.Email == expiredUser.Email))
                {
                    s.Store(expiredUser);
                }

                if (!s.Query<Application, ApplicationByNameIndex>()
                      .Any(x => x.Name == app.Name))
                {
                    s.Store(app, app.Name);
                }

                var appConfig = s.Load<Config>("1");
                if (appConfig == null)
                {
                    appConfig = new Config(new[] { "admin@email" }, new[] { "role" }, "unit testing description", "http://testurl.com/admin.html");

                    s.Store(appConfig, "1");
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
        public async Task RememberMeCookiePersistsFor2Months()
        {
            var span = DateTime.Now.AddMonths(2).Ticks;
            var login = new LoginCredentials("test@test.com", "123abc", null, true);

            var response = await _controller.UserLogin(login);
            var cookie = response.Headers.SingleOrDefault(x => x.Key == "Set-Cookie");

            Assert.That(cookie, Is.Not.Null);

            var value = cookie.Value.SingleOrDefault(x => x.StartsWith(".ASPXAUTH"));

            Assert.That(value, Is.Not.Null);

            var ticketValue = value.Remove(0, 10);
            var base64part = ticketValue.Split(new[] { ';' })[0];

            var ticket = FormsAuthentication.Decrypt(base64part);

            Assert.That(ticket.Expiration.Ticks, Is.GreaterThanOrEqualTo(span));
        }

        [Test]
        public async Task AuthCookieWithoutRememberMeLasts30Minutes()
        {
            var span = DateTime.Now.AddMinutes(31).Ticks;
            var login = new LoginCredentials("test@test.com", "123abc", null, false);

            var response = await _controller.UserLogin(login);
            var cookie = response.Headers.SingleOrDefault(x => x.Key == "Set-Cookie");
            
            Assert.That(cookie, Is.Not.Null);

            var value = cookie.Value.SingleOrDefault(x => x.StartsWith(".ASPXAUTH"));

            Assert.That(value, Is.Not.Null);

            var ticketValue = value.Remove(0, 10);
            var parts = ticketValue.Split(new[]{';'});

            var base64part = parts[0];

            var ticket = FormsAuthentication.Decrypt(base64part);

            Assert.That(ticket.Expiration.Ticks, Is.LessThan(span));
        }

        [Test]
        public async Task UserCanGetsAuthCookieWhenAuthenticated()
        {
            var login = new LoginCredentials("test@test.com", "123abc", null);

            var response = await _controller.UserLogin(login);
            var cookie = response.Headers.SingleOrDefault(x => x.Key == "Set-Cookie");

            Assert.That(cookie, Is.Not.Null);

            var value = cookie.Value.SingleOrDefault(x => x.StartsWith(".ASPXAUTH"));

            Assert.That(value, Is.Not.Null);
        }

        [Test]
        public async Task UserDoesNotGetAuthCookieWhenLoginFails()
        {
            var login = new LoginCredentials("test@test.com", "wrong", null);

            var response = await _controller.UserLogin(login);
            var cookie = response.Headers.SingleOrDefault(x => x.Key == "Set-Cookie");

            Assert.That(cookie.Value, Is.Null);
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

        [Test]
        public async Task AdminUserGetsAdminTokenOnLogin()
        {
            var login = new LoginCredentials("test@test.com", "123abc", null);

            var response = await _controller.UserLogin(login);

            var result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(result.Result, Is.Not.Null);
            Assert.That(result.Result.User.AdminToken, Is.Not.Null);
        }

        [Test]
        public async Task NormalUserDoesNotGetAdminTokenOnLogin()
        {
            var login = new LoginCredentials("notadmin@test.com", "123abc", null);

            var response = await _controller.UserLogin(login);

            var result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(result.Result, Is.Not.Null);
            Assert.That(result.Result.User.AdminToken, Is.Null);
        }

        [Test]
        public async Task AdminTokenChangesOnLogin()
        {
            var login = new LoginCredentials("test@test.com", "123abc", null);

            var response = await _controller.UserLogin(login);

            var result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(result.Result, Is.Not.Null);
            Assert.That(result.Result.User.AdminToken, Is.Not.Null);

            var adminToken = result.Result.User.AdminToken;

            response = await _controller.UserLogin(login);

            result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(result.Result, Is.Not.Null);
            Assert.That(result.Result.User.AdminToken, Is.Not.Null);

            Assert.That(adminToken, Is.Not.EqualTo(result.Result.User.AdminToken));
        }

        [TestFixture]
        public class ExpiredUsers : RavenEmbeddableTest
        {
            public override void SetUp()
            {
                base.SetUp();
                var now = DateTime.UtcNow;
                var yesterday = CommandExecutor.ExecuteCommand(new ConvertToJavascriptUtcCommand(now.AddDays(-1))).Ticks;
                var tomorrow = CommandExecutor.ExecuteCommand(new ConvertToJavascriptUtcCommand(now.AddDays(1))).Ticks;
                var twoDays = CommandExecutor.ExecuteCommand(new ConvertToJavascriptUtcCommand(now.AddDays(2))).Ticks;
                var twoDaysAgo = CommandExecutor.ExecuteCommand(new ConvertToJavascriptUtcCommand(now.AddDays(-2))).Ticks;

                var salt = CommandExecutor.ExecuteCommand(new GenerateSaltCommand());
                var password = CommandExecutor.ExecuteCommand(new HashPasswordCommand("123abc", salt, Pepper)).Result;

                var notSetUp = new User("USER", "NAME", "invalid@user.com", "AGENCY", password.HashedPassword, salt, null,
                                    "admin", "adminToken");
                var expiredUser = new User("USER", "", "too@late.com", "AGENCY", password.HashedPassword, salt, null,
                                   "publisher", null)
                {
                    AccessRules = new User.UserAccessRules
                        {
                            StartDate = twoDaysAgo,
                            EndDate = yesterday
                        }
                };
                var earlyUser = new User("USER", "", "too@early.com", "AGENCY", password.HashedPassword, salt, null,
                                   "publisher", null)
                {
                    AccessRules = new User.UserAccessRules
                    {
                        StartDate = tomorrow,
                        EndDate = twoDays
                    }
                };
                var validUser = new User("USER", "", "valid@user.com", "AGENCY", password.HashedPassword, salt, null,
                                   "publisher", null)
                {
                    AccessRules = new User.UserAccessRules
                    {
                        StartDate = twoDaysAgo,
                        EndDate = tomorrow
                    }
                };

                var users = new[] {notSetUp, expiredUser, earlyUser, validUser};

                var app = new Application(null, "test");

                using (var s = DocumentStore.OpenSession())
                {
                    foreach (var user in users)
                    {
                        if (!s.Query<User, UserByEmailIndex>()
                              .Any(x => x.Email == user.Email))
                        {
                            s.Store(user);
                        }
                    }

                    if (!s.Query<Application, ApplicationByNameIndex>()
                          .Any(x => x.Name == app.Name))
                    {
                        s.Store(app, app.Name);
                    }

                    var appConfig = s.Load<Config>("1");
                    if (appConfig == null)
                    {
                        appConfig = new Config(new[] { "admin@email" }, new[] { "role" }, "unit testing description", "http://testurl.com/admin.html")
                            {
                                UsersCanExpire = true
                            };

                        s.Store(appConfig, "1");
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

            [Test]
            public async Task ExpiredUserCanNotAuthenticate()
            {
                var login = new LoginCredentials("too@late.com", "123abc", null);

                var response = await _controller.UserLogin(login);

                var result = GetResultContent(response);

                Assert.That(result.Status, Is.EqualTo((int) HttpStatusCode.Unauthorized));
                Assert.That(result.Result, Is.Null);
            }

            [Test]
            public async Task EarlyUserCanNotAuthenticate()
            {
                var login = new LoginCredentials("too@early.com", "123abc", null);

                var response = await _controller.UserLogin(login);

                var result = GetResultContent(response);

                Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Assert.That(result.Result, Is.Null);
            }

            [Test]
            public async Task NotSetupUsersFailGracefully()
            {
                var login = new LoginCredentials("invalid@user.com", "123abc", null);

                var response = await _controller.UserLogin(login);

                var result = GetResultContent(response);

                Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Assert.That(result.Result, Is.Null);
            } 
            
            [Test]
            public async Task ValidUsersCanAuthenticate()
            {
                var login = new LoginCredentials("valid@user.com", "123abc", null);

                var response = await _controller.UserLogin(login);

                var result = GetResultContent(response);

                Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.OK));
                Assert.That(result.Result, Is.Not.Null);
                Assert.That(result.Result.Token, Is.Not.Null);
            }
        }
    }
}
