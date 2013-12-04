using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using ArcGisServerPermissionsProxy.Api.Raven.Models;

namespace ArcGisServerPermissionsProxy.Api.Models.Response.Authentication
{
    public class AuthenticationResponse
    {
        public AuthenticationResponse(TokenModel token, User user)
        {
            Token = token;
            User = user;
        }

        public User User { get; set; }

        public TokenModel Token { get; set; } 
    }
}