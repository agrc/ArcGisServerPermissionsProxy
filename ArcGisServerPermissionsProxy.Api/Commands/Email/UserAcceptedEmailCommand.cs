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

We appreciate your interest in the {{ApplicationDescription}}. Your information has been recieved but you have not been granted access yet. You will receive an email from an administrator with further instructions.

Your user name is: **{{Email}}**

Thank you for your patience,

{{ApplicationDescription}}";

            MailMessage.To.Add("test@test.com");
            MailMessage.From = new MailAddress("no-reply@utah.gov");
            MailMessage.Subject = "Registration Confirmation";
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
            public MailTemplate(string name, string email, string applicationDescription)
            {
                Name = name;
                Email = email;
                ApplicationDescription = applicationDescription;
            }

            public string Name { get; set; }
            public string Email { get; set; }
            public string ApplicationDescription { get; set; }

            public override string ToString()
            {
                return string.Format("Name: {0}, Email: {1}, ApplicationDescription: {2}", Name, Email,
                                     ApplicationDescription);
            }
        }
    }
}