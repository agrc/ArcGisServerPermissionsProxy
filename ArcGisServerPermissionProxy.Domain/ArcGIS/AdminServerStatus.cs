using System.Collections.ObjectModel;
using System.Linq;

namespace ArcGisServerPermissionsProxy.Api.Models.ArcGIS
{
    public class AdminServerStatus
    {
        public AdminServerStatus()
        {
            Messages = new Collection<string>();
        }

        public string Status { get; set; }
        public Collection<string> Messages { get; set; }

        public bool IsSuccessful
        {
            get
            {
                if (Status == "error" && Messages.All(x => x.Contains("already exists.")))
                {
                    return true;
                }

                return Status == "success";
            }
        }
    }
}