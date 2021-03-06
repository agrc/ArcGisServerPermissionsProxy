﻿using System;
using System.ComponentModel.DataAnnotations;
using Raven.Imports.Newtonsoft.Json;

namespace ArcGisServerPermissionProxy.Domain.Database {

    public class User {
        private string _application;

        public User(string firstName, string lastName, string email, string agency, string password, string salt,
                    string application, string role, string adminToken, UserAccessRules userAccessRules,
                    object additional)
        {
            Email = email.ToLowerInvariant();
            First = firstName;
            Last = lastName;
            Agency = agency;
            Password = password;
            Salt = salt;
            Application = application;
            Role = role == null ? "" : role.ToLowerInvariant();
            Approved = false;
            Active = true;
            Token = Guid.NewGuid();
            ExpirationDateTicks = DateTime.UtcNow.AddMonths(1).Ticks;
            AdminToken = adminToken;
            UserId = Guid.NewGuid();
            AccessRules = userAccessRules ?? new UserAccessRules();
            AdditionalSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(additional);
        }

        public string Id { get; set; }

        public Guid UserId { get; set; }

        /// <summary>
        ///     Gets or sets the email.
        /// </summary>
        /// <value>
        ///     The email address of the user.
        /// </value>
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        ///     Gets or sets the password.
        /// </summary>
        /// <value>
        ///     The plain text password.
        /// </value>
        [Required]
        public string Password { get; set; }

        /// <summary>
        ///     Gets or sets the application.
        /// </summary>
        /// <value>
        ///     The application or database the user exists inside.
        /// </value>
        [Required]
        public string Application
        {
            get { return _application == null ? null : _application.ToLowerInvariant(); }
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
        ///     Gets or sets the role.
        /// </summary>
        /// <value>
        ///     The role of the user.
        /// </value>
        public string Role { get; set; }

        /// <summary>
        ///     Gets or sets the salt.
        /// </summary>
        /// <value>
        ///     The salt.
        /// </value>
        public string Salt { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="User" /> is approved.
        /// </summary>
        /// <value>
        ///     <c>true</c> if approved; otherwise, <c>false</c>.
        /// </value>
        public bool Approved { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="User" /> is active.
        ///     If the user is rejected, Active will be <c>false</c>.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active { get; set; }

        /// <summary>
        ///     Gets or sets the first name.
        /// </summary>
        /// <value>
        ///     The first name.
        /// </value>
        public string First { get; set; }

        /// <summary>
        ///     Gets or sets the last name.
        /// </summary>
        /// <value>
        ///     The last name.
        /// </value>
        public string Last { get; set; }

        /// <summary>
        ///     Gets or sets the agency.
        /// </summary>
        /// <value>
        ///     The agency.
        /// </value>
        public string Agency { get; set; }

        /// <summary>
        ///     Gets or sets the token.
        /// </summary>
        /// <value>
        ///     The token used to authenticate someone.
        /// </value>
        public Guid Token { get; set; }

        /// <summary>
        ///     Gets or sets the expiration date ticks.
        /// </summary>
        /// <value>
        ///     The expiration date ticks.
        /// </value>
        public long ExpirationDateTicks { get; set; }

        /// <summary>
        ///     Gets or sets the admin token.
        /// </summary>
        /// <value>
        ///     The admin token used to validate a user as an admin for the admin endpoints.
        /// </value>
        public string AdminToken { get; set; }

        public string FullName
        {
            get { return string.Format("{0} {1}", First, Last).Trim(); }
        }

        public long LastLogin { get; set; }

        public string AdditionalSerialized { get; set; }

        [JsonIgnore]
        public object Additional
        {
            get
            {
                return string.IsNullOrEmpty(AdditionalSerialized)
                           ? null
                           : Newtonsoft.Json.JsonConvert.DeserializeObject(AdditionalSerialized);
            }
        }

        public UserAccessRules AccessRules { get; set; }

        /// <summary>
        ///     Rules class for accessing the website
        /// </summary>
        public class UserAccessRules {
            /// <summary>
            ///     Gets or sets the start date in utc ticks.
            /// </summary>
            /// <value>
            ///     The start date in ticks for when the user has access to the system.
            /// </value>
            public long StartDate { get; set; }

            /// <summary>
            ///     Gets or sets the end date in UTC ticks.
            /// </summary>
            /// <value>
            ///     The end date in ticks for when the users access expires.
            /// </value>
            public long EndDate { get; set; }

            [JsonIgnore]
            public object Options
            {
                set { OptionsSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(value); }
                get
                {
                    return string.IsNullOrEmpty(OptionsSerialized)
                               ? null
                               : Newtonsoft.Json.JsonConvert.DeserializeObject(OptionsSerialized);
                }
            }

            public string OptionsSerialized { get; set; }
        }
    }

}