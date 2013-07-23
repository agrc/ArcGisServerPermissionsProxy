using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;

namespace ArcGisServerPermissionsProxy.Api.Services.Token
{
    public class MockTokenService : ITokenService
    {
        public Task<TokenModel> GetToken(GetUsersTokenForApplicationCommandAsync.GetTokenParams tokenParams, GetUsersTokenForApplicationCommandAsync.User user, string application, string role)
        {
            return Task.Factory.StartNew(() => new TokenModel()
                {
                    Token = "you got it."
                });
        }
    }
}