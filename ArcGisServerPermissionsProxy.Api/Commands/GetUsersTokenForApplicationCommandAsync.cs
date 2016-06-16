using System.Collections.Generic;
using System.Net.Http;

namespace ArcGisServerPermissionsProxy.Api.Commands
{
    public class GetUsersTokenForApplicationCommandAsync : GetTokenCommandAsyncBase
    {
        private readonly string _application;
        private readonly string _role;

        public GetUsersTokenForApplicationCommandAsync(GetTokenParams getTokenParams, User user, string application, string role) : base(getTokenParams, user)
        {
            _application = application;
            _role = role;
        }

        public override FormUrlEncodedContent BuildPostData()
        {
            return new FormUrlEncodedContent(new[] {
                    new KeyValuePair<string, string>("username", string.Format("utah\\{0}_{1}", _application, _role)),
                    new KeyValuePair<string, string>("password", Credentials.Password),
                    new KeyValuePair<string, string>("f", "json")
                });
        }
    }
}