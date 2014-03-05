using System;
using System.Net.Http.Headers;
using System.Web.Security;

namespace ArcGisServerPermissionsProxy.Api.Services
{
    public class FormsAuthWrapper
    {
        public CookieHeaderValue SetAuthCookie(string userName, string application, bool createPersistentCookie)
        {
            var expiration = createPersistentCookie ? DateTime.Now.AddMonths(2) : DateTime.Now.AddMinutes(30);

            if (application == null)
            {
                application = "";
            }

            var ticket = new FormsAuthenticationTicket(2,
                                                       userName,
                                                       DateTime.Now,
                                                       expiration,
                                                       false,
                                                       application,
                                                       FormsAuthentication.FormsCookiePath);

            var encTicket = FormsAuthentication.Encrypt(ticket);
            
            var cookie = new CookieHeaderValue(FormsAuthentication.FormsCookieName, encTicket)
                {
                    Expires = expiration,
                    HttpOnly = true,
                    Path = FormsAuthentication.FormsCookiePath
                };

            return cookie;
        }

        public void SignOut()
        {
            FormsAuthentication.SignOut();
        }
    }
}