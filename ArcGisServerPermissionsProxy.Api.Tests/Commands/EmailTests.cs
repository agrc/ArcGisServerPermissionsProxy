using System;
using System.Dynamic;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionProxy.Domain.ViewModels;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Custom;
using CommandPattern;
using HandlebarsDotNet;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace ArcGisServerPermissionsProxy.Api.Tests.Commands
{
    public class EmailTests
    {
        [TestFixture]
        public class NewUserNotificationEmailCommandTests
        {
            private const string CondensedMarkdown = "### Hello {{Config.Description}} Administrator,\n\nWe need you to perform" +
                                                     " some administrative actions on a person that has just requested access to a site" +
                                                     " that you manage.\n\n**{{User.FullName}}**, _({{User.Email}})_, from" +
                                                     " **{{User.Agency}}** has requested access to the **{{Config.Description}}**.\n\n" +
                                                     "We need you to make sure that {{User.First}} should be allowed to access this " +
                                                     "website _and_ data. You will be able to **accept** {{User.First}} into their appropriate role " +
                                                     "and restrict {{User.First}}'s access to protected data or **reject** {{User.First}}'s " +
                                                     "request from the [user administration page]({{Config.BaseUrl}}{{Config.adminUrl}})." +
                                                     "\n\nThank you and enjoy the rest of your day!\n\n" +
                                                     "_An email will be sent to all of the other administrators after you perform one " +
                                                     "of these actions._";

            [Test]
            public void IsThereAnEmailInTheFolder()
            {
                CommandExecutor.ExecuteCommand(new NewUserAdminNotificationEmailCommand(
                                                   new NewUserAdminNotificationEmailCommand.MailTemplate(
                                                       new[] {"sgourley@utah.gov", "stdavis@utah.gov"},
                                                       new[] {"admin@application.com", "replyToList1@application.com", "replyToList2@application.com"}, "Name", "Agency",
                                                       "email@location.com", "http://url.com", "application", Guid.NewGuid(), new[] { "admin", "editor", "readonly" }, "description", "http://localhost/git/pel/src/", "user_admin.html")));
            }

            [Test]
            public void IsThereAnEmailInTheFolderWithoutALink()
            {
                CommandExecutor.ExecuteCommand(new NewUserAdminNotificationEmailCommand(
                                                   new NewUserAdminNotificationEmailCommand.MailTemplate(
                                                       new[] { "sgourley@utah.gov", "stdavis@utah.gov" },
                                                       new[] { "admin@application.com", "replyToList1@application.com", "replyToList2@application.com" }, "Name", "Agency",
                                                       "email@location.com", "http://url.com", "application", Guid.NewGuid(), new[] { "admin", "editor", "readonly" }, "description", "", "")));
            }

            [Test]
            public void CustomEmailTemplate()
            {
                var config = new Config
                {
                    AdministrativeEmails = new[] { "admin1@email.com", "admin2@email.com" },
                    Roles = new[] { "admin", "role2", "role3", "role4" },
                    Description = "unit test description",
                    AdminPage = "admin.html",
                    BaseUrl = "http://testurl.com/"
                };

                var user = new UserViewModel
                {
                    UserId = Guid.NewGuid(),
                    First = "John",
                    Last = "Brown",
                    Email = "jbrown@utah.gov",
                    Agency = "DTS",
                    Role = "Auther"
                };
 
                dynamic data = new ExpandoObject();
                data.Config = config;
                data.User = user;

                CommandExecutor.ExecuteCommand(new NotifyAdminOfNewUserEmailCommand(CondensedMarkdown, data));
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
                                                       new[] {"admin@application.com"}, "Name", "Password", "Agency")));
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
                                                       new[] { "admin@application.com" }, "Name", "role1", "UserName", "Application", "http://localhost/git/pel/src/")));
            }

            [Test]
            public void CustomEmailTemplateWithNoRestrictions()
            {
                const string condensedMarkdown = "### Hello {{User.FullName}},\n\n **Good News!** You have been" +
                                                 " granted permission to [login]({{Config.BaseUrl}}) to the " +
                                                 "{{Config.Description}}! {{#if User.AccessRules.HasRestrictions}}You have access to data in " +
                                                 "{{#each User.AccessRules.Options.County}}{{#if @first}}{{this}}{{else}}{{#if @last}} and {{this}}{{else}}, {{this}}{{/if}}{{/if}}{{/each}} until " +
                                                 "{{User.AccessRules.PrettyEndDate}}.{{else}} {{/if}}\n\n " +
                                                 "You can access the {{Config.Description}} at `{{Config.BaseUrl}}`.\n\n " +
                                                 "Your user name is: **{{User.Email}}**  \n Your assigned role is: " +
                                                 "**{{User.Role}}**  \n Your password is what you provided when you " +
                                                 "registered.  \n - _Don't worry, you can reset your password if you " +
                                                 "forgot._\n\n If you have any questions, you may reply to this " +
                                                 "email.\n\n Thank you and enjoy the rest of your day!";

                var config = new Config
                {
                    AdministrativeEmails = new[] { "admin1@email.com", "admin2@email.com" },
                    Roles = new[] { "admin", "role2", "role3", "role4" },
                    Description = "unit test description",
                    AdminPage = "admin.html",
                    BaseUrl = "http://testurl.com/"
                }; 
                var user = new UserViewModel
                {
                    UserId = Guid.NewGuid(),
                    First = "John",
                    Last = "Brown",
                    Email = "jbrown@utah.gov",
                    Agency = "DTS",
                    Role = "Auther",
                    AccessRules = new UserViewModel.UserAccessRules()
                };

                dynamic data = new ExpandoObject();
                data.Config = config;
                data.User = user;

                CommandExecutor.ExecuteCommand(new UserAcceptedNotificationEmailCommand(condensedMarkdown, data));
            }

            [Test]
            public void CustomEmailTemplateWithRestrictions()
            {
                const string condensedMarkdown = "### Hello {{User.FullName}},\n\n **Good News!** You have been" +
                                                 " granted permission to [login]({{Config.BaseUrl}}) to the " +
                                                 "{{Config.Description}}! {{#if User.AccessRules.HasRestrictions}}You have access to data in " +
                                                 "{{#each User.AccessRules.Options.County}}{{#if @first}}{{this}}{{else}}{{#if @last}} and {{this}}{{else}}, {{this}}{{/if}}{{/if}}{{/each}} until " +
                                                 "{{User.AccessRules.PrettyEndDate}}.{{else}} {{/if}}\n\n " +
                                                 "You can access the {{Config.Description}} at `{{Config.BaseUrl}}`.\n\n " +
                                                 "Your user name is: **{{User.Email}}**  \n Your assigned role is: " +
                                                 "**{{User.Role}}**  \n Your password is what you provided when you " +
                                                 "registered.  \n - _Don't worry, you can reset your password if you " +
                                                 "forgot._\n\n If you have any questions, you may reply to this " +
                                                 "email.\n\n Thank you and enjoy the rest of your day!";
                var options = new
                {
                    County = new[] { "Salt Lake", "Kane" }
                };

                var accessRules = new UserViewModel.UserAccessRules
                {
                    EndDate = 1414230242338,
                    StartDate = 1413315622096,
                    Options = options
                };

                var config = new Config
                {
                    AdministrativeEmails = new[] { "admin1@email.com", "admin2@email.com" },
                    Roles = new[] { "admin", "role2", "role3", "role4" },
                    Description = "unit test description",
                    AdminPage = "admin.html",
                    BaseUrl = "http://testurl.com/"
                }; 
                var user = new UserViewModel
                    {
                        UserId = Guid.NewGuid(),
                        First = "John",
                        Last = "Brown",
                        Email = "jbrown@utah.gov",
                        Agency = "DTS",
                        Role = "Auther",
                        AccessRules = accessRules
                    };

                dynamic data = new ExpandoObject();
                data.Config = config;
                data.User = user;

                CommandExecutor.ExecuteCommand(new UserAcceptedNotificationEmailCommand(condensedMarkdown, data));
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

        [TestFixture]
        public class HandlebarTemplateTests {
            private const string Template = "{{#each County}}" +
                                            "{{#if @first}}" +
                                            "{{this}}" +
                                            "{{else}}" +
                                            "{{#if @last}}" +
                                            " and {{this}}" +
                                            "{{else}}" +
                                            ", {{this}}" +
                                            "{{/if}}" +
                                            "{{/if}}" +
                                            "{{/each}}";

            [Test]
            public void CountyHasOneValue()
            {
                var data = new
                    {
                        County = new[] {"Kane"}
                    };

                var template = Handlebars.Compile(Template);

                var result = template(data);

                Assert.That(result, Is.EqualTo("Kane"));
            }

            [Test]
            public void CountyHasTwoValue()
            {
                var data = new
                    {
                        County = new[] {"Kane", "Salt Lake"}
                    };

                var template = Handlebars.Compile(Template);

                var result = template(data);

                Assert.That(result, Is.EqualTo("Kane and Salt Lake"));
            }

            [Test]
            public void CountyHasMoreThanTwoValue()
            {
                var data = new
                    {
                        County = new[] {"Kane", "Salt Lake", "Weber"}
                    };

                var template = Handlebars.Compile(Template);

                var result = template(data);

                Assert.That(result, Is.EqualTo("Kane, Salt Lake and Weber"));
            }

            [Test]
            public void CanUseJArrayValues()
            {
                var options = new
                    {
                        canCount = true,
                        prop = new JObject {{"County", new JArray {"Kane", "Salt Lake"}}}
                    };

                var template = Handlebars.Compile("{{#if canCount}}{{#each prop.County}}{{this}} {{/each}}{{/if}}");
                var actual = template(options);

                Assert.That(actual, Is.EqualTo("Kane Salt Lake "));
            }
        }
    }
}