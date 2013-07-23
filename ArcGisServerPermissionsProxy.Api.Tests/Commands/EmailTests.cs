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
                                                       new[] {"sgourley@utah.gov", "stdavis@utah.gov"},
                                                       new[] {"admin@application.com"}, "Name", "Agency",
                                                       "http://url.com", "application")));
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
                        new PasswordResetEmailCommand.MailTemplate(new[] {"sgourley@utah.gov", "stdavis@utah.gov"},
                                                       new[] {"admin@application.com"}, "Name", "Password", "http://url.com", "Agency")));
            }
        }

        [TestFixture]
        public class UserAcceptedEmailCommandTests
        {
            [Test]
            public void IsThereAnEmailInTheFolder()
            {
                CommandExecutor.ExecuteCommand(
                    new UserAcceptedEmailCommand(new UserAcceptedEmailCommand.MailTemplate(new[] {"sgourley@utah.gov", "stdavis@utah.gov"},
                                                       new[] {"admin@application.com"}, "Name", "role1", "UserName", "Application")));
            }
        }

        [TestFixture]
        public class UserRegistrationNotificationEmailTests
        {
            [Test]
            public void IsThereAnEmailInTheFolder()
            {
                CommandExecutor.ExecuteCommand(new UserRegistrationNotificationEmailCommand(
                                                   new UserRegistrationNotificationEmailCommand.MailTemplate(new[] {"sgourley@utah.gov", "stdavis@utah.gov"},
                                                       new[] {"admin@application.com"},"Name", "UserName", "Application")));
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
                        new UserRejectedEmailCommand.MailTemplate(new[] {"sgourley@utah.gov", "stdavis@utah.gov"},
                                                       new[] {"admin@application.com"}, "Name", "Application")));
            }
        }
    }
}