using System;
using System.ComponentModel.DataAnnotations;

namespace ArcGisServerPermissionProxy.Domain
{
    /// <summary>
    ///     A class for accepting users in the application
    /// </summary>
    public class AcceptRequestInformation : RequestInformation
    {
        public AcceptRequestInformation()
        {
        }

        public AcceptRequestInformation(string email, string role, Guid token, string application, string adminToken, long startDate = 0, long expirationDate = 0, dynamic options = null)
            : base(application, token)
        {
            Email = email;
            AdminToken = adminToken;
            StartDate = startDate;
            ExpirationDate = expirationDate;
            Options = options;
            Role = role == null ? "" : role.ToLowerInvariant();
        }

        /// <summary>
        ///     Gets or sets the email.
        /// </summary>
        /// <value>
        ///     The email of the person to get the roles for.
        /// </value>
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string AdminToken { get; set; }

        public long StartDate { get; set; }
        public long ExpirationDate { get; set; }
        public dynamic Options { get; set; }

        /// <summary>
        ///     Gets or sets the roles.
        /// </summary>
        /// <value>
        ///     The roles.
        /// </value>
        public string Role { get; set; }
    }
}