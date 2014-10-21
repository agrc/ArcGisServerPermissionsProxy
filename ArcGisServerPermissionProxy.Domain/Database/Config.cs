namespace ArcGisServerPermissionProxy.Domain.Database
{
    public class Config
    {
        /// <summary>
        /// Gets or sets the user admininstrative page.
        /// </summary>
        /// <value>
        /// This page will be appended to `BaseUrl` to access the admin page.
        /// </value>
        public string AdminPage { get; set; }

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

        /// <summary>
        /// Gets or sets the custom email markdown templates.
        /// </summary>
        /// <value>
        /// The custom email templates for an application.
        /// </value>
        public CustomEmails CustomEmails { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has custom emails.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has custom emails; otherwise, <c>false</c>.
        /// </value>
        public bool HasCustomEmails { get { return CustomEmails != null; }}
    }
}