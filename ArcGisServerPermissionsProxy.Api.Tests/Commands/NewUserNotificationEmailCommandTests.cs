using ArcGisServerPermissionsProxy.Api.Commands.Email;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Commands
{
    [TestFixture]
    public class NewUserNotificationEmailCommandTests
    {
         [Test, Explicit]
         public void IsThereAnEmailInTheFolder()
         {
             CommandExecutor.ExecuteCommand(new NewUserNotificationEmailCommand(
                                   new NewUserNotificationEmailCommand.NewUserNotificationTemplate(
                                       "test@test.com", "Name", "Agency", "ApplicationName", "http://url.com")));

         }
    }
}