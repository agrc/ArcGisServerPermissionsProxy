using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using ArcGisServerPermissionProxy.Domain;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Custom;
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
        private readonly string _approvingAdmin;

        public AcceptUserCommandAsync(IAsyncDocumentSession session, AcceptRequestInformation info,
                                     User user, string ApprovingAdmin)
        {
            _user = user;
            _approvingAdmin = ApprovingAdmin;
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
                        Options = _info.Options
                    };
            }

            await _session.SaveChangesAsync();

            if (config.HasCustomEmails && !string.IsNullOrEmpty(config.CustomEmails.NotifyUserAccepted))
            {
                dynamic data = new ExpandoObject();
                data.Config = config;
                data.User = _user;

                CommandExecutor.ExecuteCommand(
                    new UserAcceptedNotificationEmailCommand(config.CustomEmails.NotifyUserAccepted, data));
            }
            else
            {
                CommandExecutor.ExecuteCommand(
                    new UserAcceptedEmailCommand(new UserAcceptedEmailCommand.MailTemplate(new[] {_user.Email},
                                                                                           config.
                                                                                               AdministrativeEmails,
                                                                                           _user.FullName, _info.Role,
                                                                                           _user.Email,
                                                                                           config.Description,
                                                                                           config.BaseUrl)));
            }

            if (config.AdministrativeEmails.Length > 1)
            {
                var nofity = config.AdministrativeEmails.Where(x => x != _approvingAdmin).ToArray();

                CommandExecutor.ExecuteCommand(
                    new UserEngagedNotificationEmailCommand(
                        new UserEngagedNotificationEmailCommand.MailTemplate(nofity,
                                                                             _user.FullName, "Accepted", _user.Role,
                                                                             _approvingAdmin,
                                                                             config.Description)));
            }

            return null;
        }

        public override string ToString()
        {
            return string.Format("{0}, User: {1}", "AcceptUserCommandAsync", _user);
        }
    }
}