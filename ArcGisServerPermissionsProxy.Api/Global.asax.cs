using System;
using System.Configuration;
using System.Security.Policy;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using Ninject;

namespace ArcGisServerPermissionsProxy.Api
{
    public class App : HttpApplication
    {
        public static IKernel Kernel { get; set; }

        public static string Pepper
        {
            get { return ")(*&(*^%*&^$*^#$"; }
        }

        public static string Password { get; set; }

        public static AdminCredentials AdminInformation { get; set; }

        public static string Host { get; set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            AdminInformation = new AdminCredentials(ConfigurationManager.AppSettings["adminUserName"],
                                         ConfigurationManager.AppSettings["adminPassword"]);

            Password = ConfigurationManager.AppSettings["accountPassword"];
        }

        void Application_BeginRequest(Object source, EventArgs e)
        {
            var app = (HttpApplication)source;
            var context = app.Context;

            Host = CacheHost.Initialize(context);
        }

        static class CacheHost
        {
            private static string _host;

            private static readonly Object SLock = new Object();

            public static string Initialize(HttpContext context)
            {
                if (string.IsNullOrEmpty(_host))
                {
                    lock (SLock)
                    {
                        if (string.IsNullOrEmpty(_host))
                        {
                            var uri = context.Request.Url;
                            _host = uri.GetLeftPart(UriPartial.Authority);
                        }
                    }
                }

                return _host;
            }
        }
    }
}