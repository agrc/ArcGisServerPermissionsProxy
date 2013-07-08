using ArcGisServerPermissionsProxy.Api.Models.Errors;

namespace ArcGisServerPermissionsProxy.Api.Models.ArcGIS
{
    public class TokenModel : Errorable
    {
        public string Token { get; set; }
        public float Expires { get; set; }
    }
}