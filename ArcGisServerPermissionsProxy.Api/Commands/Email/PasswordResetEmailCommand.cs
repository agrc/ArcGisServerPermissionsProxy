using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class PasswordResetEmailCommand : EmailCommand
    {
        public PasswordResetEmailCommand(dynamic templateData)
        {
            TemplateData = templateData;
            MessageTemplate = @"### Dear {{Name}},

Your password has been reset to: `{{Password}}`

This new password is intended to be **temporary**. Please [change this temporary password]({{ChangePasswordUrl}}) to one of your choosing.

If you have any questions, you may reply to this email.

Thank you";

            MailMessage.To.Add("test@test.com");
            MailMessage.From = new MailAddress("no-reply@utah.gov");
            MailMessage.Subject = "Password Reset";

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public class MailTemplate : MailTemplateBase
        {
            public MailTemplate(string[] toAddresses, string[] fromAddresses, string name, string password, string changePasswordUrl, string application) : base(toAddresses, fromAddresses, name, application)
            {
                Password = password;
                ChangePasswordUrl = changePasswordUrl;
            }

            public string Password { get; set; }
            public string ChangePasswordUrl { get; set; }
        }

        public override string ToString()
        {
            return string.Format("{0}, TemplateData: {1}", "PasswordResetEmailCommand", TemplateData);
        }
    }
}