using System;
using System.ComponentModel.DataAnnotations;

namespace ArcGisServerPermissionProxy.Domain
{
    /// <summary>
        ///     A class for getting user role requests
        /// </summary>
        public class RejectRequestInformation : RequestInformation
        {
            public RejectRequestInformation(string email, Guid token, string application, string adminToken)
                : base(application, token)
            {
                Email = email;
                AdminToken = adminToken;
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
        }
}