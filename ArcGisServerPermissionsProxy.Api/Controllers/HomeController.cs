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

## Initial Setup

In the manager application add security roles.
 
Add users to those roles.   
Convention is to do `appName_accessLevel`

Setup security on folder to match");
            
            return View((object)model);
        }
    }
}