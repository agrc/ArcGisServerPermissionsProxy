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

We appreciate your interest in the {{Application}}. Your information **has been recieved** but you have **not been granted access** *yet*. You will receive an email from an administrator with further instructions.

Your user name is: `{{UserName}}`

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

        public class MailTemplate : MailTemplateBase
        {
            public MailTemplate(string[] toAddresses, string[] fromAddresses, string name, string userName, string application) : base(toAddresses, fromAddresses, name, application)
            {
                UserName = userName;
            }

            public string UserName { get; set; }
        }
    }
}