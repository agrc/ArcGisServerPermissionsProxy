using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using CommandPattern;

namespace ArcGisServerPermissionsProxy.Api.Commands
{
    public class GetTokenCommandAsync : CommandAsync<TokenModel>
    {
        private readonly Credentials _credentials;
        private const string BaseUrlFormat = "http{0}://{1}{2}/{3}/";
        private const string TokenUrlFormat = "{0}tokens/generateToken";
        private readonly UriBuilder _uriBuilder;
        private Uri _tokenUri;

        public GetTokenCommandAsync(GetTokenParams getTokenParams, Credentials credentials)
        {
            _credentials = credentials;
            var baseUrl = string.Format(BaseUrlFormat, getTokenParams.Https ? "s" : "",
                                        getTokenParams.HostName, getTokenParams.Port,
                                        getTokenParams.InstanceName);

            _uriBuilder = new UriBuilder(string.Format(TokenUrlFormat, baseUrl));
        }

        public override string ToString()
        {
            return string.Format("{0}, TokenUri: {1}", "GetTokenCommand", _tokenUri);
        }

        public override async Task<TokenModel> Execute()
        {
            _tokenUri = BuildUri();

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(_tokenUri);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

                Console.WriteLine(await response.Content.ReadAsStringAsync());

                return await response.Content.ReadAsAsync<TokenModel>(new[]
                    {
                        new TextPlainResponseFormatter()
                    });
            }
        }

        public Uri BuildUri()
        {
            var queryString = new Dictionary<string, object>
                {
                    {"username", string.Format("{0}_{1}", _credentials.Application, _credentials.Role)},
                    {"password", _credentials.Password},
                    {"f", "json"}
                };

            _uriBuilder.Query = string.Join("&", queryString.Select(x => string.Concat(
                Uri.EscapeDataString(x.Key), "=",
                Uri.EscapeDataString(x.Value.ToString()))));

            var uri = _uriBuilder.Uri;

            if (!uri.IsWellFormedOriginalString())
                throw new ArgumentException("Token url is not well formed");

            return uri;
        }

        public class Credentials
        {
            public string Application { get; set; }
            public string Role { get; set; }
            public string Password { get; set; }

            public Credentials(string application, string role, string password)
            {
                Application = application;
                Role = role;
                Password = password;
            }
        }

        public class GetTokenParams
        {
            public GetTokenParams(string hostName = "localhost", string instanceName = "arcgis",
                                         bool https = false, int? port = null)
            {
                HostName = hostName;
                InstanceName = instanceName;
                Https = https;
                Port = port.HasValue ? ":" + port.Value.ToString(CultureInfo.InvariantCulture) : "";
            }

            public string HostName { get; private set; }

            public string InstanceName { get; private set; }

            public bool Https { get; private set; }

            public string Port { get; private set; }
        }
    }
}