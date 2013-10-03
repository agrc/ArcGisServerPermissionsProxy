using System.Web.Mvc;
using MarkdownSharp;

namespace ArcGisServerPermissionsProxy.Api.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var markdown = new Markdown();
            var model = markdown.Transform(@"# ArcGIS Server Security Proxy

## Installation

- Install Raven DB `version 2375` and configure the connection string in the `web.config`
- Configure `secrets.config` in a similar fasion to `secrets.example.config` in the **Api** Project.
  - `adminUserName` is the admin account for arcgis server
  - `adminPassword` is the admin accounts password
  - `accountPassword` is the default password for all internal server accounts
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
");
            
            return View((object)model);
        }
    }
}