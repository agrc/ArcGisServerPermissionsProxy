using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using CommandPattern;

namespace ArcGisServerPermissionsProxy.Api.Services.Token
{
    public class TokenService : ITokenService
    {
        public Task<TokenModel> GetToken(GetUsersTokenForApplicationCommandAsync.GetTokenParams tokenParams, GetUsersTokenForApplicationCommandAsync.User user, string application, string role)
        {
            return CommandExecutor.ExecuteCommandAsync(new GetUsersTokenForApplicationCommandAsync(tokenParams, user, application, role));
        }
    }
}