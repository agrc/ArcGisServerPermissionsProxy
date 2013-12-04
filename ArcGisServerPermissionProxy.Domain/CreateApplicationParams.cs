using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace ArcGisServerPermissionProxy.Domain
{
    public class CreateApplicationParams
    {
        [Required]
        public string Application { get; set; }

        [Required]
        public string[] AdminEmails { get; set; }

        [Required]
        public Collection<string> Roles { get; set; }

        [Required]
        public string CreationToken { get; set; }
    }
}