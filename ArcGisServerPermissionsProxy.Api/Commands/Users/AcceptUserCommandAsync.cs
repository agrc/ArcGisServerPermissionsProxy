using System.Linq;
using System.Threading.Tasks;
using ArcGisServerPermissionProxy.Domain;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using CommandPattern;
using Newtonsoft.Json;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Commands.Users
{
    public class AcceptUserCommandAsync : CommandAsync<string>
    {
        private readonly AcceptRequestInformation _info;
        private readonly IAsyncDocumentSession _session;
        private readonly User _user;

        public AcceptUserCommandAsync(IAsyncDocumentSession session, AcceptRequestInformation info,
                                     User user)
        {
            _user = user;
            _session = session;
            _info = info;
        }

        public override async Task<string> Execute()
        {
            var config = await _session.LoadAsync<Config>("1");

            if (_user == null)
            {
                return "User was not found.";
            }

            if (!config.Roles.Contains(_info.Role.ToLowerInvariant()))
            {
                return "Role was not found.";
            }

            _user.Active = true;
            _user.Approved = true;
            _user.Role = _info.Role;

            if (config.UsersCanExpire)
            {
                _user.AccessRules = new User.UserAccessRules
                    {
                        StartDate = _info.StartDate,
                        EndDate = _info.ExpirationDate,
                        OptionsSerialized = JsonConvert.SerializeObject(_info.Options)
                    };
            }

            await _session.SaveChangesAsync();

            CommandExecutor.ExecuteCommand(
                new UserAcceptedEmailCommand(new UserAcceptedEmailCommand.MailTemplate(new[] {_user.Email},
                                                                                       config.
                                                                                           AdministrativeEmails,
                                                                                       _user.FullName, _info.Role,
                                                                                       _user.Email,
                                                                                       config.Description)));

            return null;
        }

        public override string ToString()
        {
            return string.Format("{0}, User: {1}", "AcceptUserCommandAsync", _user);
        }
    }
}