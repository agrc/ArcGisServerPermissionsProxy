using System;
using System.Net.Http.Headers;
using System.Web.Security;

namespace ArcGisServerPermissionsProxy.Api.Services
{
    public class FormsAuthWrapper : IFormsAuthentication
    {
        public CookieHeaderValue SetAuthCookie(string userName, bool createPersistentCookie)
        {
            var expiration = createPersistentCookie ? DateTime.Now.AddMonths(2) : DateTime.Now.AddMinutes(30);

            var ticket = new FormsAuthenticationTicket(2,
                                                       userName,
                                                       DateTime.Now,
                                                       expiration,
                                                       false,
                                                       userName,
                                                       FormsAuthentication.FormsCookiePath);

            var encTicket = FormsAuthentication.Encrypt(ticket);

            return new CookieHeaderValue(FormsAuthentication.FormsCookieName, encTicket);
            //FormsAuthentication.SetAuthCookie(userName, createPersistentCookie);
        }

        public void SignOut()
        {
            FormsAuthentication.SignOut();
        }
    }

    public interface IFormsAuthentication
    {
        void SignOut();

        CookieHeaderValue SetAuthCookie(string userName, bool createPersistentCookie);
    }
}