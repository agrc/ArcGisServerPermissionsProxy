using System.Net;
using ArcGisServerPermissionsProxy.Api.Raven.Configuration;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using Ninject.Activation;
using Ninject.Modules;
using Raven.Client;
using Raven.Client.Document;

namespace ArcGisServerPermissionsProxy.Api.Configuration.Ninject.Modules
{
    public class RavenModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IDocumentStore>()
                .ToMethod(Init)
                .InSingletonScope();

            Bind<IDatabaseExists>().To<DatabaseExists>();
            Bind<IIndexable>().To<Indexable>();
        }

        private static IDocumentStore Init(IContext context)
        {
            var documentStore = new DocumentStore
            {
                ConnectionStringName = "RavenDb"
            }.Initialize();

            RavenConfig.Register(typeof(UserByIdIndex), documentStore);

            return documentStore;
        }
    }
}