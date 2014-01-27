using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using CommandPattern;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Commands.Query
{
    public class GetUserByTokenCommandAsync : CommandAsync<User>
    {
        private readonly Guid _token;
        private readonly IAsyncDocumentSession _session;

        public override string ToString()
        {
            return string.Format("{0}, Token: {1}", "GetUserByTokenCommandAsync", _token);
        }

        public GetUserByTokenCommandAsync(Guid token, IAsyncDocumentSession session)
        {
            _token = token;
            _session = session;
        }

        public override async Task<User> Execute()
        {
            var users = await _session.Query<User, UserByEmailIndex>()
                                      .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                                      .Where(x => x.Token == _token)
                                      .ToListAsync();

            User user = null;
            try
            {
                user = users.Single();
            }
            catch (InvalidOperationException)
            {
                return user;
            }

            return user;
        }
    }
}