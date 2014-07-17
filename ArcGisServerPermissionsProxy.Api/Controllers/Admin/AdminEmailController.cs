using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using ArcGisServerPermissionProxy.Domain;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionsProxy.Api.Commands.Query;
using ArcGisServerPermissionsProxy.Api.Commands.Users;
using ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure;
using ArcGisServerPermissionsProxy.Api.Models.ViewModels;
using CommandPattern;

namespace ArcGisServerPermissionsProxy.Api.Controllers.Admin
{
    public class AdminEmailController : RavenController
    {
        [HttpGet]
        public async Task<ActionResult> Accept(string application, string role, Guid token)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(application) || string.IsNullOrEmpty(role) ||
                token == Guid.Empty)
            {
                return View(new AdminEmailViewModel("Email Approval", "Invalid parameters", null));
            }

            Database = application;

            using (var s = AsyncSession)
            {
                var config = await s.LoadAsync<Config>("1");
                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserByTokenCommandAsync(token, s));

                if (user == null)
                {
                    return View(new AdminEmailViewModel("Email Approval", "User not found.", null));
                }

                if (user.Token != token)
                {
                    return View(new AdminEmailViewModel(config.Description, "Incorrect token.", user));
                }

                if (user.ExpirationDateTicks < DateTime.Now.Ticks)
                {
                    return View(new AdminEmailViewModel(config.Description, "This token has expired after one month of inactivity.", user));
                }

                var info = new AcceptRequestInformation(user.Email, role, token, application, null);

                var response =
                    await CommandExecutor.ExecuteCommandAsync(new AcceptUserCommandAsync(s, info, user, "an Admin email link"));

                if (response != null)
                {
                    return View(new AdminEmailViewModel(config.Description, response, user));
                }

                return View(new AdminEmailViewModel(config.Description, user));
            }
        }

        [HttpGet]
        public async Task<ActionResult> Reject(string application, Guid token)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(application) || token == Guid.Empty)
            {
                return View(new AdminEmailViewModel("Email Approval", "Invalid parameters", null));
            }

            Database = application;

            using (var s = AsyncSession)
            {
                var config = await s.LoadAsync<Config>("1");
                var user = await CommandExecutor.ExecuteCommandAsync(new GetUserByTokenCommandAsync(token, s));

                if (user == null)
                {
                    return View(new AdminEmailViewModel("Email Approval", "User not found.", null));
                }

                if (user.Token != token)
                {
                    return View(new AdminEmailViewModel(config.Description, "Incorrect token.", user));
                }

                if (user.ExpirationDateTicks < DateTime.Now.Ticks)
                {
                    return View(new AdminEmailViewModel(config.Description, "This token has expired after one month of inactivity.", user));
                }

                await CommandExecutor.ExecuteCommandAsync(new RejectUserCommandAsync(s, user, "an Admin email link"));


                return View(new AdminEmailViewModel(config.Description, user));
            }
        }

    }
}