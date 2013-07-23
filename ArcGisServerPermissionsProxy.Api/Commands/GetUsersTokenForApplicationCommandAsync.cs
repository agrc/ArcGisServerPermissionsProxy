using System;
using System.Collections.Generic;
using System.Linq;

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

        public override Uri BuildUri()
        {
            var queryString = new Dictionary<string, object>
                {
                    {"username", string.Format("{0}_{1}", _application, _role)},
                    {"password", Credentials.Password},
                    {"f", "json"}
                };

            UriBuilder.Query = string.Join("&", queryString.Select(x => string.Concat(
                Uri.EscapeDataString(x.Key), "=",
                Uri.EscapeDataString(x.Value.ToString()))));

            var uri = UriBuilder.Uri;

            if (!uri.IsWellFormedOriginalString())
                throw new ArgumentException("Token url is not well formed");

            return uri;
        }
    }
}