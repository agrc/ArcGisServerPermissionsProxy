using Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api.Models.Response.Password
{
    public class PasswordReset
    {
        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; } 
    }
}