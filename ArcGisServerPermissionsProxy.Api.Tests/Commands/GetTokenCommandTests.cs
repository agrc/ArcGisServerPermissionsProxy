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
                var command = new GetTokenCommand(new GetTokenCommand.GetTokenParams(),
                                                  new GetTokenCommand.Credentials("security_role_1", "admin", "wrong"));

                var actual = await CommandExecutor.ExecuteCommandAsync(command);

                Assert.That(actual, Is.Not.Null);
                Assert.That(actual.Successful, Is.False);
                Assert.That(actual.Error.Message, Is.Not.Null);
            }

            [Test]
            public async Task CanGetTokenWithValidCredentials()
            {
                var command = new GetTokenCommand(new GetTokenCommand.GetTokenParams(),
                                                  new GetTokenCommand.Credentials("security_role_1", "admin", "test"));

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
                var command = new GetTokenCommand(new GetTokenCommand.GetTokenParams(),
                                                  new GetTokenCommand.Credentials("app1", "admin", "test"));

                var actual = command.BuildUri();

                Assert.That(actual.ToString(),
                            Is.EqualTo(
                                "http://localhost/arcgis/tokens/generateToken?username=app1_admin&password=test&f=json"));
            }

            [Test]
            public void ComplexUrlGetCreatedCorrectly()
            {
                var command =
                    new GetTokenCommand(new GetTokenCommand.GetTokenParams("localhost", "arcgis", true, 6080),
                                        new GetTokenCommand.Credentials("app1", "admin", "test"));
                var actual = command.BuildUri();

                Assert.That(actual.ToString(),
                            Is.EqualTo(
                                "https://localhost:6080/arcgis/tokens/generateToken?username=app1_admin&password=test&f=json"));
            }
        }
    }
}