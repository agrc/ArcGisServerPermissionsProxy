using System.Linq;
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

This new password is intended to be **temporary**. Please change this temporary password to one of your choosing.

If you have any questions, you may reply to this email.

Thank you";

            MailMessage.To.Add(string.Join(",", templateData.ToAddresses));
            MailMessage.From = new MailAddress(Enumerable.First(templateData.FromAddresses));

            if (templateData.FromAddresses.Length > 1)
            {
                MailMessage.CC.Add(string.Join(",", Enumerable.Skip(templateData.FromAddresses, 1)));
            }
            
            MailMessage.Subject = "Password Reset";

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public override string ToString()
        {
            return string.Format("{0}, TemplateData: {1}", "PasswordResetEmailCommand", TemplateData);
        }

        public class MailTemplate : MailTemplateBase
        {
            public MailTemplate(string[] toAddresses, string[] fromAddresses, string name, string password, string application)
                : base(toAddresses, fromAddresses, name, application)
            {
                Password = password;
            }

            public string Password { get; set; }
        }
    }
}