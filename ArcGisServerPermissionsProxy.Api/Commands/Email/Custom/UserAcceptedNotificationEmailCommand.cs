using System.Linq;
using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email.Custom {

    public class UserAcceptedNotificationEmailCommand : EmailCommand {
        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }
        
        public UserAcceptedNotificationEmailCommand(string templateString, dynamic values)
        {
            MessageTemplate = templateString;
            TemplateData = values;

            MailMessage.To.Add(TemplateData.User.Email);
            MailMessage.From = new MailAddress(Enumerable.First(TemplateData.Config.AdministrativeEmails));

            foreach (var replyTo in TemplateData.Config.AdministrativeEmails)
            {
                MailMessage.ReplyToList.Add(replyTo);
            }

            MailMessage.Subject = string.Format("You have been granted access to {0}", TemplateData.Config.Description);
            
            Init();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}, template: {1}, data: {2}", "UserAcceptedNotificationEmailCommand", MessageTemplate, TemplateData);
        }
    }

}