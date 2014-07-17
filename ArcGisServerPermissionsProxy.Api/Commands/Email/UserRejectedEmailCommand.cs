using System.Linq;
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

We appreciate your interest in the {{Application}}; however, your request for access has been **rejected**. If you find this email to be in error, please respond and explain the error.

Thank you for understanding.";

            MailMessage.To.Add(string.Join(",", templateData.ToAddresses));
            MailMessage.From = new MailAddress(Enumerable.First(templateData.FromAddresses));

            if (templateData.FromAddresses.Length > 1)
            {
                foreach (var replyTo in templateData.FromAddresses)
                {
                    MailMessage.ReplyToList.Add(replyTo);
                }
            }

            MailMessage.Subject = string.Format("{0} - Access Reject", templateData.Application);

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public override string ToString()
        {
            return string.Format("{0}, TemplateData: {1}", "UserRejectedEmailCommand", TemplateData);
        }

        public class MailTemplate : MailTemplateBase
        {
            public MailTemplate(string[] toAddresses, string[] fromAddresses, string name, string application)
                : base(toAddresses, fromAddresses, name, application)
            {
            }
        }
    }
}