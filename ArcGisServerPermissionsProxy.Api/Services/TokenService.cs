using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using CommandPattern;

namespace ArcGisServerPermissionsProxy.Api.Services
{
    public class TokenService : ITokenService
    {
        public Task<TokenModel> GetToken(GetTokenCommandAsync.GetTokenParams tokenParams, GetTokenCommandAsync.Credentials credentials)
        {
            return CommandExecutor.ExecuteCommandAsync(new GetTokenCommandAsync(tokenParams, credentials));
        }
    }
}