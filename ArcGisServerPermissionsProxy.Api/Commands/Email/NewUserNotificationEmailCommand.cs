using System.Net.Mail;
using System.Net.Mime;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;
using Nustache.Core;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class NewUserNotificationEmailCommand : EmailCommand
    {
        private const string Template = @"### Dear Admins,

{{name}} from {{agency}} has requested access to the {{application}} web application. 
    
Please use [the user admin page]({{url}}) to accept or reject their request.";
        private readonly NewUserNotificationTemplate _templateData;

        public NewUserNotificationEmailCommand(NewUserNotificationTemplate templateData)
        {
            _templateData = templateData;
        }      

        public override string ToString()
        {
            return string.Format("{0}, NewUser: {1}", "NewUserNotificationEmailCommandAsync", _templateData);
        }

        public override void Execute()
        {
            var markup = Render.StringToString(Template, _templateData);
            var markdown = Markdowner.Transform(markup);

            MailMessage.To.Add("test@test.com");
            MailMessage.From = new MailAddress("no-reply@utah.gov");
            MailMessage.Body = markup;
            MailMessage.Subject = "Notification of Registration";
            MailMessage.AlternateViews.Add(AlternateView(markup, MediaTypeNames.Text.Plain));
            MailMessage.AlternateViews.Add(AlternateView(markdown, MediaTypeNames.Text.Html));

            Mailman.SendAsync(MailMessage, "sending");

            MailMessage.Dispose();
        }

        public class NewUserNotificationTemplate
        {
            public NewUserNotificationTemplate(string to, string name, string agency, string application, string url)
            {
                To = to;
                Name = name;
                Agency = agency;
                Application = application;
                Url = url;
            }

            public string To { get; set; }
            public string Name { get; set; }
            public string Agency { get; set; }
            public string Application { get; set; }
            public string Url { get; set; }
        }
    }
}