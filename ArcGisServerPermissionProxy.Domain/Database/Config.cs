using System.Collections.Generic;
using System.Linq;

namespace ArcGisServerPermissionProxy.Domain.Database
{
    public class Config
    {
        public Config(string[] administrativeEmails, IEnumerable<string> roles, string description, string adminUrl, string baseUrl)
        {
            AdministrativeEmails = administrativeEmails;
            Description = description;
            AdminUrl = adminUrl;
            BaseUrl = baseUrl;
            Roles = roles.Select(x=>x.ToLowerInvariant()).ToArray();
            UsersCanExpire = false;
        }

        /// <summary>
        /// Gets or sets the user admininstrative URL.
        /// </summary>
        /// <value>
        /// This page will be appended to `BaseUrl` to access the admin page.
        /// </value>
        public string AdminUrl { get; set; }

        /// <summary>
        /// Gets or sets the base URL of the application.
        /// </summary>
        /// <value>
        /// The base URL.
        /// </value>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the administrative emails. These email adresses will become admin users
        /// and will have their passwords sent to them.
        /// </summary>
        /// <value>
        /// The administrative emails.
        /// </value>
        public string[] AdministrativeEmails { get; set; }

        /// <summary>
        /// Gets or sets the description that is used in emails ect.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the roles for the aplication.
        /// </summary>
        /// <value>
        /// The roles.
        /// </value>
        public string[] Roles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether users can expire.
        /// </summary>
        /// <value>
        ///   If true, then the login must check times as a part of the login process.
        /// </value>
        public bool UsersCanExpire { get; set; }
    }
}