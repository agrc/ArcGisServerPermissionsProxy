using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class UserRejectedEmailCommand : EmailCommand
    {
        public UserRejectedEmailCommand(dynamic templateData)
        {
            TemplateData = templateData;
            MessageTemplate = @"###Dear {{Name}},

We appreciate your interest in the {{Application}} application; however, your request for access has been **rejected**. If you find this email to be in error, please respond and explain the error.

Thank you for understanding.";

            MailMessage.To.Add("test@test.com");
            MailMessage.From = new MailAddress("no-reply@utah.gov");
            MailMessage.Subject = "Access Reject";

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public override string ToString()
        {
            return string.Format("{0}, TemplateData: {1}", "UserRejectedEmailCommand", TemplateData);
        }

        public class MailTemplate
        {
            public MailTemplate(string name, string email, string application)
            {
                Name = name;
                Application = application;
                Email = email;
            }

            public string Name { get; set; }
            public string Application { get; set; }
            public string Email { get; set; }
        }
    }
}