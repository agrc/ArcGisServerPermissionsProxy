using System;
using System.ComponentModel.DataAnnotations;

namespace ArcGisServerPermissionProxy.Domain
{
    /// <summary>
    ///     A class encapsulating common request paramaters
    /// </summary>
    public class RequestInformation
    {
        private string _application;

        public RequestInformation()
        {
        }

        public RequestInformation(string application, Guid token)
        {
            Application = application;
            Token = token;
        }

        /// <summary>
        ///     Gets the database.
        /// </summary>
        /// <value>
        ///     The database or application name of the user.
        /// </value>
        [Required]
        public string Application
        {
            get { return _application == null ? null : _application.ToLowerInvariant(); }
            set
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
        ///     Gets the token.
        /// </summary>
        /// <value>
        ///     The token arcgis server generated.
        /// </value>
        public Guid Token { get; protected set; }
    }
}