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

            var ticket = new FormsAuthenticationTicket(2,
                                                       userName,
                                                       DateTime.Now,
                                                       expiration,
                                                       false,
                                                       application,
                                                       FormsAuthentication.FormsCookiePath);

            var encTicket = FormsAuthentication.Encrypt(ticket);

            return new CookieHeaderValue(FormsAuthentication.FormsCookieName, encTicket);
        }

        public void SignOut()
        {
            FormsAuthentication.SignOut();
        }
    }
}