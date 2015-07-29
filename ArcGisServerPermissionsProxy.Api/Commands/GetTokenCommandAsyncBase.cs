using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;
using CommandPattern;
using NLog;

namespace ArcGisServerPermissionsProxy.Api.Commands
{
    public abstract class GetTokenCommandAsyncBase : CommandAsync<TokenModel>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected readonly User Credentials;
        private const string BaseUrlFormat = "http{0}://{1}{2}/{3}/";
        private const string TokenUrlFormat = "{0}tokens/generateToken";
        protected readonly UriBuilder UriBuilder;
        private Uri _tokenUri;

        protected GetTokenCommandAsyncBase(GetTokenParams getTokenParams, User credentials)
        {
            Credentials = credentials;
            var baseUrl = string.Format(BaseUrlFormat, getTokenParams.Https ? "s" : "",
                                        getTokenParams.HostName, getTokenParams.Port,
                                        getTokenParams.InstanceName);

            UriBuilder = new UriBuilder(string.Format(TokenUrlFormat, baseUrl));
            Logger.Info("Get Token url: {0}, Formatting: {1}", baseUrl, TokenUrlFormat);
        }

        public override string ToString()
        {
            return string.Format("{0}, TokenUri: {1}", "GetTokenCommand", _tokenUri);
        }

        public override async Task<TokenModel> Execute()
        {
            _tokenUri = UriBuilder.Uri;

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(_tokenUri, BuildPostData());

                Logger.Info("Get token status code: {0}", response.StatusCode);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

//                var content = await response.Content.ReadAsStringAsync();
//
//                Logger.Info(content);
//                Console.WriteLine(content);

                return await response.Content.ReadAsAsync<TokenModel>(new[]
                    {
                        new TextPlainResponseFormatter()
                    });
            }
        }

        public virtual FormUrlEncodedContent BuildPostData()
        {
            return new FormUrlEncodedContent(new []
                {
                    new KeyValuePair<string, string>("username", Credentials.Username),
                    new KeyValuePair<string, string>("password", Credentials.Password),
                    new KeyValuePair<string, string>("f", "json")
                });
        }

        public class User
        {
            public string Username { get; set; }
            public string Password { get; set; }

            public User(string username, string password)
            {
                Username = username;
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