using Ninject.Modules;

namespace ArcGisServerPermissionsProxy.Api.Configuration.Ninject.Modules
{
    public class BootstrapArcGisServerSecurityCommandAsyncModule : NinjectModule
    {
        public override void Load()
        {
            Bind<BootstrapArcGisServerSecurityCommandAsyncModule>().ToSelf();
        }
    }
}