using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ArcGisServerPermissionsProxy.Api.Commands.Email;
using ArcGisServerPermissionsProxy.Api.Controllers.Admin;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using CommandPattern;
using System.Linq;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Commands.Users
{
    public class AcceptUserCommandAsync : CommandAsync<HttpResponseMessage>
    {
        private readonly HttpRequestMessage _request;
        private readonly User _user;
        private readonly IAsyncDocumentSession _session;
        private readonly AdminController.AcceptRequestInformation _info;

        public AcceptUserCommandAsync(IAsyncDocumentSession session, AdminController.AcceptRequestInformation info, HttpRequestMessage request, User user)
        {
            _request = request;
            _user = user;
            _session = session;
            _info = info;
        }

        public override async Task<HttpResponseMessage> Execute()
        {
            var config = await _session.LoadAsync<Config>("1");

            if (_user == null)
            {
                return _request.CreateResponse(HttpStatusCode.NotFound, new ResponseContainer(HttpStatusCode.NotFound, "User was not found."));
            }

            if (!config.Roles.Contains(_info.Role.ToLowerInvariant()))
            {
               return _request.CreateResponse(HttpStatusCode.NotFound, new ResponseContainer(HttpStatusCode.NotFound, "Role was not found."));
            }

            _user.Active = true;
            _user.Approved = true;
            _user.Role = _info.Role;

            await _session.SaveChangesAsync();

            Task.Factory.StartNew(
                () =>
                CommandExecutor.ExecuteCommand(
                    new UserAcceptedEmailCommand(new UserAcceptedEmailCommand.MailTemplate(new[] { _user.Email },
                                                                                           config.
                                                                                               AdministrativeEmails,
                                                                                           _user.Name, _info.Role,
                                                                                           _user.Email,
                                                                                           _user.Application))));

            return null;
        }

        public override string ToString()
        {
            throw new System.NotImplementedException();
        }
    }
}