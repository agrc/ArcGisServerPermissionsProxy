using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Commands;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;

namespace ArcGisServerPermissionsProxy.Api.Services.Token
{
    public interface ITokenService
    {
        Task<TokenModel> GetToken(GetUsersTokenForApplicationCommandAsync.GetTokenParams tokenParams,
                                  GetUsersTokenForApplicationCommandAsync.User user, string application, string role);
    }
}