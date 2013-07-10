using System.Net.Mail;
using System.Net.Mime;
using CommandPattern;
using MarkdownSharp;
using Nustache.Core;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class NewUserNotificationEmailCommand : Command
    {
        private const string Template = @"### Dear Admins,

{{name}} from {{agency}} has requested access to the {{application}} web application. 
    
Please use [the user admin page]({{url}}) to accept or reject their request.";
        private readonly NewUserNotificationTemplate _templateData;
        private readonly MailMessage _mailMessage;
        private readonly Markdown _markdowner;
        private readonly SmtpClient _mailman;

        public NewUserNotificationEmailCommand(NewUserNotificationTemplate templateData)
        {
            _templateData = templateData;

            _mailman = new SmtpClient();
            _mailMessage = new MailMessage
                {
                    IsBodyHtml = true
                };

            _markdowner = new Markdown();
        }

        public override string ToString()
        {
            return string.Format("{0}, NewUser: {1}", "NewUserNotificationEmailCommandAsync", _templateData);
        }

        public override void Execute()
        {
            var markup = Render.StringToString(Template, _templateData);
            var markdown = _markdowner.Transform(markup);

            _mailMessage.To.Add("test@test.com");
            _mailMessage.From = new MailAddress("no-reply@utah.gov");
            _mailMessage.Body = markup;
            _mailMessage.Subject = "Notification of Registration";
            _mailMessage.AlternateViews.Add(AlternateView(markup, MediaTypeNames.Text.Plain));
            _mailMessage.AlternateViews.Add(AlternateView(markdown, MediaTypeNames.Text.Html));

            _mailman.SendAsync(_mailMessage, "sending");

            _mailMessage.Dispose();
        }

        private static AlternateView AlternateView(string text, string mediaType)
        {
            return System.Net.Mail.AlternateView.CreateAlternateViewFromString(text, null, mediaType);
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