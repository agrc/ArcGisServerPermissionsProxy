using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace ArcGisServerPermissionProxy.Domain
{
    public class CreateApplicationParams
    {
        public class ApplicationInfo
        {
            public ApplicationInfo(string name, string description)
            {
                Name = name;
                Description = description;
            }

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
        }

        [Required]
        public ApplicationInfo Application { get; set; }

        [Required]
        public string[] AdminEmails { get; set; }

        [Required]
        public Collection<string> Roles { get; set; }

        [Required]
        public string CreationToken { get; set; }
    }
}