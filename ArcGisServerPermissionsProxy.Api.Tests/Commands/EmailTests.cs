using ArcGisServerPermissionsProxy.Api.Commands.Email;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Commands
{
    public class EmailTests
    {
        [TestFixture]
        public class NewUserNotificationEmailCommandTests
        {
            [Test]
            public void IsThereAnEmailInTheFolder()
            {
                CommandExecutor.ExecuteCommand(new NewUserNotificationEmailCommand(
                                                   new NewUserNotificationEmailCommand.MailTemplate(
                                                       "test@test.com", "Name", "Agency", "ApplicationName",
                                                       "http://url.com")));

            }
        }

        [TestFixture]
        public class PasswordResetEmailCommandTests
        {
            [Test]
            public void IsThereAnEmailInTheFolder()
            {
                CommandExecutor.ExecuteCommand(
                    new PasswordResetEmailCommand(
                        new PasswordResetEmailCommand.MailTemplate("Name", "Email",
                                                                   "password", new[] {"emails"}, "url")));
            }
        }

        [TestFixture]
        public class UserAcceptedEmailCommandTests
        {
            [Test]
            public void IsThereAnEmailInTheFolder()
            {
                CommandExecutor.ExecuteCommand(
                        new UserAcceptedEmailCommand(new UserAcceptedEmailCommand.MailTemplate("Name", "Email",
                                                                                               "description", new[]{"multiple","roles"})));
            }
        }

        [TestFixture]
        public class UserRejectedEmailCommandTests
        {
            [Test]
            public void IsThereAnEmailInTheFolder()
            {
                CommandExecutor.ExecuteCommand(
                                           new UserRejectedEmailCommand(
                                               new UserRejectedEmailCommand.MailTemplate("Name",
                                                                                         "Email", "Application")));
            }
        }

        [TestFixture]
        public class UserRegistrationNotificationEmailTests
        {
            [Test]
            public void IsThereAnEmailInTheFolder()
            {
                CommandExecutor.ExecuteCommand(new UserRegistrationNotificationEmailCommand(
                                                   new UserRegistrationNotificationEmailCommand.MailTemplate(
                                                       "sgourley@utah.gov", "Name", "ApplicationName")));
            }
        }
    }
}