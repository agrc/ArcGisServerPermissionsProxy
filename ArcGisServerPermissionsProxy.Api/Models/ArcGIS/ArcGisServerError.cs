using Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api.Models.ArcGIS
{
    public class ArcGisServerError
    {
        [JsonProperty(PropertyName = "code")]
        public int Code { get; set; }
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
        [JsonProperty(PropertyName = "details")]
        public string Details { get; set; }
    }
}