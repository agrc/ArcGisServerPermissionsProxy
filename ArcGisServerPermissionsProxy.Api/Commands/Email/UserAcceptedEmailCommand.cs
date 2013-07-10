using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class UserAcceptedEmailCommand : EmailCommand
    {
        public UserAcceptedEmailCommand(MailTemplate templateData)
        {
            TemplateData = templateData;
            MessageTemplate = @"### Dear {{Name}},

You have been granted permission to login to the {{Application}} web application.

Your user name is `{{email}}`  
Your assigned role is: `{{#roles}}{{.}} {{/roles}}`

If you have any questions, you may reply to this email.

Thank you";

            MailMessage.To.Add("test@test.com");
            MailMessage.From = new MailAddress("no-reply@utah.gov");
            MailMessage.Subject = "Access Granted";

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public override string ToString()
        {
            return string.Format("{0}, Template: {1}, MessageTemplate: {2}", "UserAcceptedEmailCommand", TemplateData,
                                 MessageTemplate);
        }

        public class MailTemplate
        {
            public MailTemplate(string name, string email, string application, string[] roles)
            {
                Name = name;
                Email = email;
                Application = application;
                Roles = roles;
            }

            public string Name { get; set; }
            public string[] Role { get; set; }
            public string Email { get; set; }
            public string Application { get; set; }
            public string[] Roles { get; set; }

            public override string ToString()
            {
                return string.Format("Name: {0}, Role: {1}, Email: {2}, ApplicationDescription: {3}", Name, Role, Email, Application);
            }
        }
    }
}