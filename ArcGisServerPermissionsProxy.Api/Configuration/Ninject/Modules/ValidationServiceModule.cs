using ArcGisServerPermissionsProxy.Api.Services;
using Ninject.Modules;
using Ninject.Web.Common;

namespace ArcGisServerPermissionsProxy.Api.Configuration.Ninject.Modules
{
    public class ValidationServiceModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IValidationService>().To<ValidationService>().InRequestScope();
        }
    }
}