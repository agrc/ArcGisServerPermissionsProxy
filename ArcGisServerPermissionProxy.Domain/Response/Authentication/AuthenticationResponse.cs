using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionProxy.Domain.ViewModels;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;

namespace ArcGisServerPermissionProxy.Domain.Response.Authentication
{
    public class AuthenticationResponse
    {
        public AuthenticationResponse(TokenModel token, User user)
        {
            Token = token;
            User = AutoMapper.Mapper.Map<User, UserViewModel>(user);
        }

        public UserViewModel User { get; set; }

        public TokenModel Token { get; set; } 
    }
}