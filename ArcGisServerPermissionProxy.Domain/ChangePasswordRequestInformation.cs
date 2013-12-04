using System;
using System.ComponentModel.DataAnnotations;

namespace ArcGisServerPermissionProxy.Domain
{
    public class ChangePasswordRequestInformation : RequestInformation
    {
        public ChangePasswordRequestInformation(string email, string currentPassword, string newPassword,
                                                string newPasswordRepeated, string application, Guid token)
            : base(application, token)
        {
            CurrentPassword = currentPassword;
            NewPassword = newPassword;
            NewPasswordRepeated = newPasswordRepeated;
            Email = email;
        }

        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }

        [Required]
        public string NewPasswordRepeated { get; set; }

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