using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ArcGisServerPermissionsProxy.Api.Raven.Models
{
    public class User
    {
        private string _application;

        public User(string name, string email, string agency, string password, string salt, string application, IEnumerable<string> roles)
        {
            Email = email.ToLowerInvariant();
            Name = name;
            Agency = agency;
            Password = password;
            Salt = salt;
            Application = application;
            Roles = roles.Select(x=>x.ToLowerInvariant()).ToArray();
            Approved = false;
            Active = true;
        }

        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>
        /// The email address of the user.
        /// </value>
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The plain text password.
        /// </value>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the application.
        /// </summary>
        /// <value>
        /// The application or database the user exists inside.
        /// </value>
        [Required]
        public string Application
        {
            get { return _application.ToLowerInvariant(); }
            private set
            {
                if (value == null || string.IsNullOrEmpty(value))
                    _application = null;
                else
                {
                    _application = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        /// <value>
        /// The role of the user.
        /// </value>
        public string[] Roles { get; set; }

        /// <summary>
        /// Gets or sets the salt.
        /// </summary>
        /// <value>
        /// The salt.
        /// </value>
        public string Salt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="User"/> is approved.
        /// </summary>
        /// <value>
        ///   <c>true</c> if approved; otherwise, <c>false</c>.
        /// </value>
        public bool Approved { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="User"/> is active.
        /// If the user is rejected, Active will be <c>false</c>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the agency.
        /// </summary>
        /// <value>
        /// The agency.
        /// </value>
        public string Agency { get; set; }
    }
}