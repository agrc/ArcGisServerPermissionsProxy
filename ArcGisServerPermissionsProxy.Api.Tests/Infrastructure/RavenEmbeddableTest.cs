using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
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

//            DocumentStore = new DocumentStore
//                {
//                    DefaultDatabase = "ArcGisSecurity",
//                    Url = "http://localhost:8079"
//                }.Initialize();

            IndexCreation.CreateIndexes(typeof(UserByEmailIndex).Assembly, DocumentStore);
        }

        [TearDown]
        public virtual void TearDown()
        {
            DocumentStore.Dispose();
        }
    }
}