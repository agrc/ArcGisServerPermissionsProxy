﻿using AgrcPasswordManagement.Models.Account;

namespace ArcGisServerPermissionProxy.Domain.Account
{
    public class Credentials : LoginCredentials
    {
        public Credentials(string email, string password, string application)
            : base(email, password, application)
        {
        }

        public string First { get; set; }

        public string Last { get; set; }

        public string Agency { get; set; }

        public string FullName
        {
            get { return string.Format("{0} {1}", First, Last).Trim(); }
        }
    }
}