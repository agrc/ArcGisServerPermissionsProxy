using System.Web.Mvc;
using MarkdownSharp;

namespace ArcGisServerPermissionsProxy.Api.Controllers {

  public class HomeController : Controller {
    private string _transform;
    private string _debugText;
    private string _releaseText;

    public ActionResult Index()
    {
      var markdown = new Markdown();
      var model = "";
      const string license = "<div style='padding-top:100px;font-size:8px;color:#eee;' data-license-type='creative-commons-attribution'> <ul> <li class='license'><a href='http://creativecommons.org/licenses/by/3.0/us/' target='_blank'>Creative Commons – Attribution (CC BY 3.0) <i class='ui_cc'></i></a></li> <li class='attribution'>Identity Protection designed by <a href='http://www.thenounproject.com/grubedoo'>Jason Grube</a> from the <a href='http://www.thenounproject.com'>Noun Project</a></li> <li class='print-attribution hidden'>Identity Protection designed by Jason Grube from the thenounproject.com</li> </ul> </div>";
      _releaseText = string.Format("<div style='margin: 50px auto 0 auto;width:500px;text-align:center'><h1>Permission Proxy</h1><img src='{0}' width='500px' height='500px'>{1}</div>", Url.Content("~/Content/icon.png"), license);
      _debugText = @"# ArcGIS Server Security Proxy

## Installation

- Install Raven DB `version 2375` and configure the connection string in the `web.config`
- Configure `secrets.config` in a similar fasion to `secrets.example.config` in the **Api** Project.
  - `adminUserName` is the admin account for arcgis server
  - `adminPassword` is the admin accounts password
  - `accountPassword` is the default password for all internal server accounts  
  - `creationToken` is the super top secret password you use to send requests to the admin api  
  - `serverUrl` is the base url of your server eg: http://localhost:6080
- Configure the SMTP pickup directory/Network settings in the `web.config`

## Running Tests

- Configure `secrets.config` in a similar fasion to `secrets.example.config` in the **Tests** Project.
  - `adminUserName` is the admin account for arcgis server
  - `adminPassword` is the admin accounts password
  - `accountPassword` is the default password for all internal server accounts
- Run the **Explicit** `CreatesUsersRolesAndAssignsUsersToRoles` test function in `BootstrapArcGisServerSecurityCommandTests.cs`. 
  - This will create roles and users in arcgis server.

## Usage 

### (Refer to the chrome postman extension for routes and parameters)

## Create the application

`/api/admin/createapplication`

_It is optional to setup the permissions on arcgis server folders and services_

_An `admin` role will be automatically generated_

## Register Users

`/api/user/register`

An email will be placed in the pickup location or sent over the network. 

## Approve first user by email
- Click the `accept as role` link in the email

## List applied users

`/api/user/getallwaiting`

## Approve users from list

`/api/admin/accept`

## Reject users

`/api/admin/reject`

## Login
`/api/authenticate/user`

## Password management

`/api/user/resetpassword`

`/api/user/changepassword`
";
#if DEBUG
      _transform = markdown.Transform(_debugText);
#endif
#if !DEBUG
      _transform = _releaseText;
#endif
      model = _transform;

      return View((object) model);
    }
  }

}