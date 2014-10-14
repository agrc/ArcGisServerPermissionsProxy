using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email.Custom {

    public class NotifyAdminOfNewUserEmailCommand : EmailCommand {
        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }
        
        public NotifyAdminOfNewUserEmailCommand(string templateString, dynamic values)
        {
            MessageTemplate = templateString;
            TemplateData = values;

            MailMessage.To.Add(string.Join(",", TemplateData.Config.AdministrativeEmails));
            MailMessage.From = new MailAddress("no-reply@utah.gov" );

            MailMessage.Subject = string.Format("{0} - Notification of Registration", TemplateData.Config.Description);

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
            return string.Format("{0}, template: {1}, data: {2}", "NotifyAdminOfNewUserEmailCommand", MessageTemplate, TemplateData);
        }
    }

}