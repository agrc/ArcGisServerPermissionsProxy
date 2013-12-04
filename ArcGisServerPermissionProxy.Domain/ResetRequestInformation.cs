using System;
using System.ComponentModel.DataAnnotations;

namespace ArcGisServerPermissionProxy.Domain
{
    /// <summary>
    ///     A class for reseting a users password
    /// </summary>
    public class ResetRequestInformation : RequestInformation
    {
        public ResetRequestInformation(string email, string application, Guid token)
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