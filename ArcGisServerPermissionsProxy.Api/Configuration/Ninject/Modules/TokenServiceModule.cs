using ArcGisServerPermissionsProxy.Api.Services;
using Ninject.Modules;
using Ninject.Web.Common;

namespace ArcGisServerPermissionsProxy.Api.Configuration.Ninject.Modules
{
    public class TokenServiceModule : NinjectModule
    {
        public override void Load()
        {
#if !DEBUG
            Bind<ITokenService>().To<TokenService>().InRequestScope();
#endif
        }
    }
}