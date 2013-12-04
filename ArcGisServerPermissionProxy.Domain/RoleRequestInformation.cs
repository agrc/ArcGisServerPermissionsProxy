using System;
using System.ComponentModel.DataAnnotations;

namespace ArcGisServerPermissionProxy.Domain
{
    /// <summary>
    ///     A class for getting user role requests
    /// </summary>
    public class RoleRequestInformation : RequestInformation
    {
        public RoleRequestInformation()
        {
        }

        public RoleRequestInformation(string email, string application, Guid token)
            : base(application, token)
        {
            Email = email;
        }

        /// <summary>
        ///     Gets or sets the email.
        /// </summary>
        /// <value>
        ///     The email of the person to get the roles for.
        /// </value>
        [EmailAddress]
        public string Email { get; set; }
    }
}