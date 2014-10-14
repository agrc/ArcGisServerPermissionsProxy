using System;
using System.Globalization;
using Newtonsoft.Json;

namespace ArcGisServerPermissionProxy.Domain.ViewModels {

    public class UserViewModel
    {
        public Guid UserId { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public string Email { get; set; }
        public string Agency { get; set; }
        public string Role { get; set; }
        public string AdminToken { get; set; }
        public string LastLogin { get; set; }
        public object Additional { get; set; }
        public UserAccessRules AccessRules { get; set; }
        public string Application { get; set; }

        public class UserAccessRules
        {
            public long StartDate { get; set; }
            public long EndDate { get; set; }
            public object Options { get; set; }
            /// <summary>
            /// Gets the pretty start date for use in emails.
            /// </summary>
            /// <value>
            /// The pretty start date.
            /// </value>
            [JsonIgnore]
            public string PrettyStartDate
            {
                get
                {
                    if (StartDate <= 0)
                    {
                        return "now";
                    }
                    var ticks = (StartDate * 10000) + 621355968000000000;
                    return new DateTime(ticks).ToString(CultureInfo.InvariantCulture);
                }
            }
            [JsonIgnore]
            public string PrettyEndDate
            {
                get
                {
                    if (EndDate <= 0)
                    {
                        return "unknown";
                    }
                    var ticks = (EndDate * 10000) + 621355968000000000;
                    return new DateTime(ticks).ToString(CultureInfo.InvariantCulture);
                }
            }

            [JsonIgnore]
            public bool HasRestrictions { get { return StartDate > 0 || EndDate > 0 || Options != null; } }
        }
    }

}