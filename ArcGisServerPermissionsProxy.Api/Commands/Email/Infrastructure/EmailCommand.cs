using System.Net.Mail;
using System.Net.Mime;
using CommandPattern;
using MarkdownSharp;
using Nustache.Core;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure
{
    public abstract class EmailCommand : Command
    {
        protected MailMessage MailMessage;
        protected SmtpClient Mailman;
        protected Markdown Markdowner;
        private string _html;
        private string _plainText;

        protected EmailCommand()
        {
            Init();
        }

        public abstract string MessageTemplate { get; protected internal set; }
        public abstract dynamic TemplateData { get; protected internal set; }

        public void Init()
        {
            Mailman = new SmtpClient();
            MailMessage = new MailMessage
                {
                    IsBodyHtml = true
                };

            Markdowner = new Markdown();
            _plainText = Render.StringToString(MessageTemplate, TemplateData);
            _html = Markdowner.Transform(_plainText);

            MailMessage.Body = _html;
            MailMessage.AlternateViews.Add(AlternateView(_plainText, MediaTypeNames.Text.Plain));
            MailMessage.AlternateViews.Add(AlternateView(_html, MediaTypeNames.Text.Html));
        }

        protected virtual AlternateView AlternateView(string text, string mediaType)
        {
            return System.Net.Mail.AlternateView.CreateAlternateViewFromString(text, null, mediaType);
        }

        public override void Execute()
        {
            Mailman.SendAsync(MailMessage, "sending");
        }

        public override void Run()
        {
            base.Run();

            MailMessage.Dispose();
        }
    }
}