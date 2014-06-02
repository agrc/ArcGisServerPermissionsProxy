using System;
using System.Configuration;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using ArcGisServerPermissionsProxy.Api.Models.Account;
using NLog;
using Ninject;

namespace ArcGisServerPermissionsProxy.Api {

    public class App : HttpApplication {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static IKernel Kernel { get; set; }

        public static string Pepper
        {
            get { return ")(*&(*^%*&^$*^#$"; }
        }

        public static string Password { get; set; }
        public static AdminCredentials AdminInformation { get; set; }
        public static string Host { get; set; }
        public static string ArcGisHostUrl { get; set; }
        public static string Instance { get; set; }
        public static int Port { get; set; }
        public static bool Ssl { get; set; }
        public static string CreationToken { get; set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            Cache();
        }

        public static void Cache()
        {

            AdminInformation = new AdminCredentials(ConfigurationManager.AppSettings["adminUserName"],
                                                    ConfigurationManager.AppSettings["adminPassword"]);

            Password = ConfigurationManager.AppSettings["accountPassword"];
            ArcGisHostUrl = ConfigurationManager.AppSettings["host"];
            Instance = ConfigurationManager.AppSettings["instance"] ?? "localhost";
            Port = Convert.ToInt16(ConfigurationManager.AppSettings["port"]);
            Ssl = Convert.ToBoolean(ConfigurationManager.AppSettings["SSL"]);
            CreationToken = ConfigurationManager.AppSettings["creationToken"];

            Logger.Info(
                "App Startup {0}Host: {1}{0}ArcGisHost: {2}{0}Instance: {3}{0}Port: {4}{0}SSL: {5}{0}CreationToken: {6}{0}",
                Environment.NewLine, Host, ArcGisHostUrl, Instance, Port, Ssl, CreationToken);
        }

        private void Application_BeginRequest(Object source, EventArgs e)
        {
            var app = (HttpApplication) source;
            var context = app.Context;

            Host = CacheHost.Initialize(context);
        }

        private static class CacheHost {
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