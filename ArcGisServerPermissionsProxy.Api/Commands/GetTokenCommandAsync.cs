namespace ArcGisServerPermissionsProxy.Api.Commands
{
    public class GetTokenCommandAsync : GetTokenCommandAsyncBase
    {
        public GetTokenCommandAsync(GetTokenParams getTokenParams, User credentials) : base(getTokenParams, credentials)
        {
        }
    }
}