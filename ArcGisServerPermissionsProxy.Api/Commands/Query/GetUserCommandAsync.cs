using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using CommandPattern;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Commands.Query
{
    public class GetUserCommandAsync : CommandAsync<User>
    {
        public string Email { get; set; }
        public IAsyncDocumentSession Session { get; set; }

        public GetUserCommandAsync(string email, IAsyncDocumentSession session)
        {
            Email = email;
            Session = session;
        }

        public override string ToString()
        {
            return string.Format("{0}, Email: {1}", "GetUserCommandAsync", Email);
        }

        public override async Task<User> Execute()
        {
            var users = await Session.Query<User, UserByEmailIndex>()
                               .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                               .Where(x => x.Email == Email.ToLowerInvariant())
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