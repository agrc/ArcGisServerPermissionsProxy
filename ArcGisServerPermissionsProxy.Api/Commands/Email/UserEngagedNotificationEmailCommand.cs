using System.Linq;
using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class UserEngagedNotificationEmailCommand : EmailCommand
    {
        public UserEngagedNotificationEmailCommand(dynamic templateData)
        {
            TemplateData = templateData;
            MessageTemplate = @"### Dear Administrator,

`{{UserName}}` was **{{Status}}** by {{ApprovingAdmin}}{{Role}}.

Please ignore the email asking for you to perform an action on {{UserName}} unless you disagree with this action.";

            MailMessage.To.Add(string.Join(",", templateData.ToAddresses));
            MailMessage.From = new MailAddress("no-reply@utah.gov");
            MailMessage.Subject = string.Format("{0} - {1} was {2}{3}", templateData.Application, templateData.UserName, templateData.Status, templateData.Role);

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
            public MailTemplate(string[] toAddresses, string userName, string status, string role, string approvingAdmin, string application) : base(toAddresses, null, null, application)
            {
                UserName = userName;
                Status = status;
                if (!string.IsNullOrEmpty(role))
                {
                    role = " as a " + role;
                }
                Role = role;
                ApprovingAdmin = approvingAdmin;
            }

            public string Status { get; set; }
            public string Role { get; set; }
            public string ApprovingAdmin { get; set; }
            public string UserName { get; set; }
        }
    }
}