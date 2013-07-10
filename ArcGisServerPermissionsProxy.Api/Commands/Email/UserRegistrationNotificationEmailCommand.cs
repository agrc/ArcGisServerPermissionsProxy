using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class UserRegistrationNotificationEmailCommand : EmailCommand
    {
        public UserRegistrationNotificationEmailCommand(dynamic templateData)
        {
            TemplateData = templateData;
            MessageTemplate = @"### Dear {{Name}},

We appreciate your interest in the {{ApplicationDescription}}. Your information **has been recieved** but you have **not been granted access** *yet*. You will receive an email from an administrator with further instructions.

Your user name is: `{{Email}}`

Thank you for your patience.";

            MailMessage.To.Add("test@test.com");
            MailMessage.From = new MailAddress("no-reply@utah.gov");
            MailMessage.Subject = "Registration Confirmation";

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public override string ToString()
        {
            return string.Format("{0}, TemplateData: {1}", "UserRegistrationNotificationEmailCommand", TemplateData);
        }

        public class MailTemplate
        {
            public MailTemplate(string name, string email, string application)
            {
                Name = name;
                Email = email;
                Application = application;
            }

            public string Name { get; set; }
            public string Email { get; set; }
            public string Application { get; set; }

            public override string ToString()
            {
                return string.Format("Name: {0}, Email: {1}, Application: {2}", Name, Email, Application);
            }
        }
    }
}