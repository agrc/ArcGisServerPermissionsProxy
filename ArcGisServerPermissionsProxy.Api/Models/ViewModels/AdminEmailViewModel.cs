using ArcGisServerPermissionProxy.Domain.Database;

namespace ArcGisServerPermissionsProxy.Api.Models.ViewModels
{
    public class AdminEmailViewModel
    {
        public AdminEmailViewModel(string description, User user)
        {
            Description = description;
            User = user;
        }

        /// <summary>
        /// Gets or sets the error message to display on the page.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public string ErrorMessage { get; set; }

        public AdminEmailViewModel(string description, string errorMessage, User user)
        {
            Description = description;
            ErrorMessage = errorMessage;
            User = user;
        }

        /// <summary>
        /// Gets or sets the long description of the application.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the user be administrated.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        public User User { get; set; }
    }
}