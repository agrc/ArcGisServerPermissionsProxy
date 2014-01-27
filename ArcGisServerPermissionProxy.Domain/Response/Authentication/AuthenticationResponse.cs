using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;

namespace ArcGisServerPermissionProxy.Domain.Response.Authentication
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