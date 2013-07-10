﻿using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class UserAcceptedEmailCommand : EmailCommand
    {
        public UserAcceptedEmailCommand(MailTemplate templateData)
        {
            TemplateData = templateData;
            MessageTemplate = @"### Dear {{Name}},

You have been granted permission to login to the {{Application}} web application.

Your user name is `{{username}}`  
Your assigned role is: `{{#roles}}{{.}} {{/roles}}`

If you have any questions, you may reply to this email.

Thank you";

            MailMessage.To.Add("test@test.com");
            MailMessage.From = new MailAddress("no-reply@utah.gov");
            MailMessage.Subject = "Access Granted";

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public override string ToString()
        {
            return string.Format("{0}, Template: {1}, MessageTemplate: {2}", "UserAcceptedEmailCommand", TemplateData,
                                 MessageTemplate);
        }

        public class MailTemplate : MailTemplateBase
        {
            public MailTemplate(string[] toAddresses, string[] fromAddresses, string name, string[] role,
                                string userName, string application)
                : base(toAddresses, fromAddresses, name, application)
            {
                UserName = userName;
                Roles = role;
            }

            public string[] Roles { get; set; }
            public string UserName { get; set; }

            public override string ToString()
            {
                return string.Format("{0}, Roles: {1}, UserName: {2}", base.ToString(), string.Join(", ", Roles),
                                     UserName);
            }
        }
    }
}