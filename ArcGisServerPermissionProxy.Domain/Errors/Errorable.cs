using ArcGisServerPermissionProxy.Domain.ArcGIS;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api.Models.Errors
{
    public class Errorable
    {
        [JsonProperty(PropertyName = "error")]
        public ArcGisServerError Error { get; set; }

        public bool Successful
        {
            get { return Error == null || string.IsNullOrEmpty(Error.Message); }
        }
    }
}