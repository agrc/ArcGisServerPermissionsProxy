using System.Threading.Tasks;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using CommandPattern;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Commands.Users
{
    public class RejectUserCommandAsync : CommandAsync
    {
        private readonly IAsyncDocumentSession _s;
        private readonly User _user;

        public RejectUserCommandAsync(IAsyncDocumentSession s, User user)
        {
            _user = user;
            _s = s;
        }

        public override async Task<bool> Execute()
        {
            _user.Active = false;
            _user.Approved = false;
            _user.Role = null;

            await _s.SaveChangesAsync();

            var config = await _s.LoadAsync<Config>("1");

            CommandExecutor.ExecuteCommand(
                new UserRejectedEmailCommand(new UserRejectedEmailCommand.MailTemplate(new[] {_user.Email},
                                                                                       config.AdministrativeEmails,
                                                                                       _user.FullName,
                                                                                       _user.Application)));

            return true;
        }

        public override string ToString()
        {
            return string.Format("{0}, User: {1}", "RejectUserCommandAsync", _user);
        }
    }
}