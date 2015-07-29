using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using Raven.Database.Server;

namespace ArcGisServerPermissionsProxy.Api.Tests.Infrastructure
{
    public abstract class RavenEmbeddableTest
    {
        public IDocumentStore DocumentStore { get; set; }

        [SetUp]
        public virtual void SetUp()
        {
            NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(8081);
            DocumentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true,
                UseEmbeddedHttpServer = true
            }.Initialize();

            IndexCreation.CreateIndexes(typeof(UserByEmailIndex).Assembly, DocumentStore);
        }

        [TearDown]
        public virtual void TearDown()
        {
            DocumentStore.Dispose();
        }
    }
}