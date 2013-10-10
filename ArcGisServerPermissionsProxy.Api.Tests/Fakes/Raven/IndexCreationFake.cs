using System.ComponentModel.Composition.Hosting;
using ArcGisServerPermissionsProxy.Api.Raven.Configuration;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Tests.Fakes.Raven
{
    public class IndexCreationFake : IIndexable
    {
        public void CreateIndexes(CatalogExportProvider provider, IDocumentStore store, string database)
        {
            global::Raven.Client.Indexes.IndexCreation.CreateIndexes(provider, store.DatabaseCommands, store.Conventions);

        }
    }
}