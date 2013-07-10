using System.Linq;
using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class NewUserNotificationEmailCommand : EmailCommand
    {
        public NewUserNotificationEmailCommand(dynamic templateData)
        {
            TemplateData = templateData;
            MessageTemplate = @"### Dear Admin,

{{name}} from {{agency}} has requested access to the {{application}} web application. 
    
Please use [the user admin page]({{url}}) to **accept** or **reject** their request.";

            MailMessage.To.Add(string.Join(",", templateData.ToAddresses));
            MailMessage.From = new MailAddress(Enumerable.First(templateData.FromAddresses));

            if (templateData.FromAddresses.Length > 1)
            {
                MailMessage.CC.Add(string.Join(",", Enumerable.Skip(templateData.FromAddresses, 1)));
            }

            MailMessage.Subject = "Notification of Registration";

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public override string ToString()
        {
            return string.Format("{0}, NewUser: {1}", "NewUserNotificationEmailCommandAsync", TemplateData);
        }

        public class MailTemplate : MailTemplateBase
        {
            public MailTemplate(string[] toAddresses, string[] fromAddresses, string name, string agency, string url, string application) : base(toAddresses, fromAddresses, name, application)
            {
                Agency = agency;
                Url = url;
            }

            public string Agency { get; set; }
            public string Url { get; set; }

            public override string ToString()
            {
                return string.Format("Agency: {0}, Url: {1}", Agency, Url);
            }
        }
    }
}