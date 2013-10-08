using ArcGisServerPermissionsProxy.Api.Models.Errors;
using Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api.Models.ArcGIS
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TokenModel : Errorable
    {
        [JsonProperty]
        public string Token { get; set; }

        [JsonProperty]
        public float Expires { get; set; }
    }
}