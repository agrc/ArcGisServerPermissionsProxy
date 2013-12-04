using System.Collections.Generic;
using System.Linq;

namespace ArcGisServerPermissionsProxy.Api.Raven.Models
{
    public class Config
    {
        public Config(string[] administrativeEmails, IEnumerable<string> roles)
        {
            AdministrativeEmails = administrativeEmails;
            Roles = roles.Select(x=>x.ToLowerInvariant()).ToArray();
        }

        public string[] AdministrativeEmails { get; set; }
        public string[] Roles { get; set; }
    }
}