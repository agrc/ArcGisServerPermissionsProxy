using ArcGisServerPermissionsProxy.Api.Services;
using Ninject.Modules;
using Ninject.Web.Common;

namespace ArcGisServerPermissionsProxy.Api.Configuration.Ninject.Modules {

    public class UrlHelperModule : NinjectModule {
        /// <summary>
        /// Loads the module into the kernel.
        /// </summary>
        public override void Load()
        {
            Bind<IUrlBuilder>().To<UrlBuilder>().InRequestScope();
        }
    }

}