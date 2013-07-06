using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Client.Indexes;

namespace ArcGisServerPermissionsProxy.Api.Tests.Infrastructure
{
    public abstract class RavenEmbeddableTest
    {
        public IDocumentStore DocumentStore { get; set; }

        [SetUp]
        public virtual void SetUp()
        {
            DocumentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true
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