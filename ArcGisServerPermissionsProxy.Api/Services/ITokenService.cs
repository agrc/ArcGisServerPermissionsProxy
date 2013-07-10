using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;

namespace ArcGisServerPermissionsProxy.Api.Services
{
    public interface ITokenService
    {
        Task<TokenModel> GetToken(GetTokenCommandAsync.GetTokenParams tokenParams,
                                  GetTokenCommandAsync.Credentials credentials);
    }
}