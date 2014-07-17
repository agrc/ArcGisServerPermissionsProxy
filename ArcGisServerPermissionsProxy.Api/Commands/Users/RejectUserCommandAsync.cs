using System.Linq;
using System.Threading.Tasks;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using CommandPattern;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Commands.Users
{
    public class RejectUserCommandAsync : CommandAsync
    {
        private readonly IAsyncDocumentSession _s;
        private readonly User _user;
        private readonly string _rejectingUser;

        public RejectUserCommandAsync(IAsyncDocumentSession s, User user, string rejectingUser)
        {
            _user = user;
            _rejectingUser = rejectingUser;
            _s = s;
        }

        public override async Task<bool> Execute()
        {
            if (_user.Active == false && 
                _user.Approved == false &&
                string.IsNullOrEmpty(_user.Role))
            {
                // do not notify if nothing is changing.
                return true;
            }

            _user.Active = false;
            _user.Approved = false;
            _user.Role = null;

            await _s.SaveChangesAsync();

            var config = await _s.LoadAsync<Config>("1");

            CommandExecutor.ExecuteCommand(
                new UserRejectedEmailCommand(new UserRejectedEmailCommand.MailTemplate(new[] {_user.Email},
                                                                                       config.AdministrativeEmails,
                                                                                       _user.FullName,
                                                                                       config.Description)));

            if (config.AdministrativeEmails.Length > 1)
            {
                var nofity = config.AdministrativeEmails.Where(x => x != _rejectingUser).ToArray();

                CommandExecutor.ExecuteCommand(
                    new UserEngagedNotificationEmailCommand(
                        new UserEngagedNotificationEmailCommand.MailTemplate(nofity,
                                                                             _user.FullName, "Rejected", _user.Role,
                                                                             _rejectingUser,
                                                                             config.Description)));
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("{0}, User: {1}", "RejectUserCommandAsync", _user);
        }
    }
}