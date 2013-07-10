using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;

namespace ArcGisServerPermissionsProxy.Api.Services
{
    public class MockTokenService : ITokenService
    {
        public Task<TokenModel> GetToken(GetTokenCommandAsync.GetTokenParams tokenParams, GetTokenCommandAsync.Credentials credentials)
        {
            return Task.Factory.StartNew(() => new TokenModel()
                {
                    Token = "you got it."
                });
        }
    }
}