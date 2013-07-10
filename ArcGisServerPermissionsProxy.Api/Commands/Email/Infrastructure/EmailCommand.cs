using System.Net.Mail;
using CommandPattern;
using MarkdownSharp;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure
{
    public abstract class EmailCommand : Command
    {
        protected MailMessage MailMessage;
        protected Markdown Markdowner;
        protected SmtpClient Mailman;

        protected EmailCommand()
        {
            Init();
        }

        public void Init()
        {
            Mailman = new SmtpClient();
            MailMessage = new MailMessage
                {
                    IsBodyHtml = true
                };

            Markdowner = new Markdown();
        }

        protected virtual AlternateView AlternateView(string text, string mediaType)
        {
            return System.Net.Mail.AlternateView.CreateAlternateViewFromString(text, null, mediaType);
        }
    }
}