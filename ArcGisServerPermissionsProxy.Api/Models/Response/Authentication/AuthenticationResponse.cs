namespace ArcGisServerPermissionsProxy.Api.Models.Response
{
    public class AuthenticationResponse
    {
        public AuthenticationResponse(string token)
        {
            Token = token;
        }

        public string Token { get; set; } 
    }
}