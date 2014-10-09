using System;
using System.ComponentModel.DataAnnotations;
using ArcGisServerPermissionProxy.Domain.Database;

namespace ArcGisServerPermissionProxy.Domain.Account {
    public class UpdateUser {
        private string _application;

        [Required]
        public string Application
        {
            get
            {
                if (_application == null || string.IsNullOrEmpty(_application))
                    return null;

                return _application.ToLower();
            }
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

        [Required]
        public string AdminToken { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public string Agency { get; set; }
        public object Additional { get; set; }
        public User.UserAccessRules AccessRules { get; set; }
    }

}