using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Commands;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Commands
{
    public class GetTokenCommandTests
    {
        [TestFixture]
        public class IntegrationTests
        {
            [Test]
            public async Task CanGetErrorMessageWithInvalidCredentials()
            {
                var command = new GetUsersTokenForApplicationCommandAsync(new GetTokenCommandAsyncBase.GetTokenParams(),
                                                  new GetTokenCommandAsyncBase.User(null, "wrong"), "app1", "role");

                var actual = await CommandExecutor.ExecuteCommandAsync(command);

                Assert.That(actual, Is.Not.Null);
                Assert.That(actual.Successful, Is.False);
                Assert.That(actual.Error.Message, Is.Not.Null);
            }

            [Test]
            public async Task CanGetTokenWithValidCredentials()
            {
                var command = new GetUsersTokenForApplicationCommandAsync(new GetTokenCommandAsyncBase.GetTokenParams(),
                                                  new GetTokenCommandAsyncBase.User(null, "test"), "app1", "role");

                var actual = await CommandExecutor.ExecuteCommandAsync(command);

                Assert.That(actual, Is.Not.Null);
                Assert.That(actual.Successful, Is.True);
                Assert.That(actual.Token, Is.Not.Null);
            }
        }

        [TestFixture]
        public class UrlTests
        {
            [Test]
            public void BasicUrlGetCreatedCorrectly()
            {
                var command = new GetUsersTokenForApplicationCommandAsync(new GetTokenCommandAsyncBase.GetTokenParams(),
                                                  new GetTokenCommandAsyncBase.User(null, "test"), "app1", "role");

                var actual = command.BuildUri();

                Assert.That(actual.ToString(),
                            Is.EqualTo(
                                "http://localhost/arcgis/tokens/generateToken?username=app1_role&password=test&f=json"));
            }

            [Test]
            public void ComplexUrlGetCreatedCorrectly()
            {
                var command =
                    new GetUsersTokenForApplicationCommandAsync(new GetTokenCommandAsyncBase.GetTokenParams("localhost", "arcgis", true, 6080),
                                        new GetTokenCommandAsyncBase.User(null, "test"), "app1", "role");
                var actual = command.BuildUri();

                Assert.That(actual.ToString(),
                            Is.EqualTo(
                                "https://localhost:6080/arcgis/tokens/generateToken?username=app1_role&password=test&f=json"));
            }
        }
    }
}