using Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api.Models.Response.Authentication
{
    public class AuthenticationResponse
    {
        public AuthenticationResponse(string token)
        {
            Token = token;
        }

        [JsonProperty(PropertyName="token")]
        public string Token { get; set; } 
    }
}