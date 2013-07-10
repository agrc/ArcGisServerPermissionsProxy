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

This new password is intended to be **temporary**. Please [change this temporary password]({{ChangePasswordUrl}} to a password of your choosing.

If you have any questions, you may reply to this email.

Thank you";

            MailMessage.To.Add("test@test.com");
            MailMessage.From = new MailAddress("no-reply@utah.gov");
            MailMessage.Subject = "Registration Confirmation";
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public class MailTemplate
        {
            public MailTemplate(string email, string name, string password, string[] adminEmails, string changePasswordUrl)
            {
                Email = email;
                Name = name;
                Password = password;
                AdminEmails = adminEmails;
                ChangePasswordUrl = changePasswordUrl;
            }

            public string Email { get; set; }
            public string Name { get; set; }
            public string Password { get; set; }
            public string[] AdminEmails { get; set; }
            public string ChangePasswordUrl { get; set; }

            public override string ToString()
            {
                return string.Format("Email: {0}, Name: {1}, Password: {2}, AdminEmails: {3}, ChangePasswordUrl: {4}", Email, Name, Password, AdminEmails, ChangePasswordUrl);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}, TemplateData: {1}", "PasswordResetEmailCommand", TemplateData);
        }
    }
}