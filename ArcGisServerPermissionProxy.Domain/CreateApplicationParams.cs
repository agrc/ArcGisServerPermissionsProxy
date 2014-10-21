using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ArcGisServerPermissionProxy.Domain
{
    public class CreateApplicationParams
    {
        private Collection<string> _roles;

        public class ApplicationInfo
        {
            public ApplicationInfo()
            {
                
            }
            public ApplicationInfo(string name, string description)
            {
                Name = name;
                Description = description;
            }

            /// <summary>
            /// Gets or sets the user admininstration URL for the website.
            /// </summary>
            /// <value>
            /// To create the admin URL it is concatenated with `BaseUrl`.
            /// </value>
            public string AdminPage { get; set; }

            /// <summary>
            /// Gets or sets the base URL to the application.
            /// </summary>
            /// <value>
            /// The base URL.
            /// </value>
            public string BaseUrl { get; set; }

            /// <summary>
            /// Gets or sets the name of the application.
            /// This will be used to identify the application
            /// </summary>
            /// <value>
            /// The unique name of the application.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the description.
            /// This will be used in emails and other places where the name isn't appropriate
            /// </summary>
            /// <value>
            /// The description.
            /// </value>
            public string Description { get; set; }

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>
            /// A string that represents the current object.
            /// </returns>
            public override string ToString()
            {
                return string.Format("Name: {0}, Description: {1}", Name, Description);
            }

            /// <summary>
            /// Gets or sets a value indicating whether there are[access rules].
            /// </summary>
            /// <value>
            ///   <c>true</c> if users will have time related or other access rules.
            /// </value>
            public bool AccessRules { get; set; }

            /// <summary>
            /// Gets or sets the custom email markdown.
            /// </summary>
            /// <value>
            /// The custom emails.
            /// </value>
            public CustomEmailMarkdown CustomEmails { get; set; }

            public class CustomEmailMarkdown {
                public string NotifyAdminOfNewUser { get; set; }
                public string NotifyUserAccepted { get; set; }
            }
        }

        [Required]
        public ApplicationInfo Application { get; set; }

        [Required]
        public string[] AdminEmails { get; set; }

        [Required]
        public Collection<string> Roles
        {
            get { return _roles; }
            set
            {
                _roles = value;

                if (!_roles.Select(x => x.ToLower()).Contains("admin"))
                {
                    _roles.Add("admin");
                }
            }
        }

        [Required]
        public string CreationToken { get; set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Application: {0}, AdminEmails: {1}, Roles: {2}, CreationToken: {3}", Application, string.Join(",", AdminEmails), string.Join(",",Roles), CreationToken);
        }
    }
}