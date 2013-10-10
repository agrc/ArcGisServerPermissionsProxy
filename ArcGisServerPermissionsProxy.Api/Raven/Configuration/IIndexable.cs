using System.ComponentModel.Composition.Hosting;
using Raven.Client;
using Raven.Client.Indexes;

namespace ArcGisServerPermissionsProxy.Api.Raven.Configuration
{
    public interface IIndexable
    {
        void CreateIndexes(CatalogExportProvider provider, IDocumentStore store, string database);
    }

    public class Indexable : IIndexable
    {
        public void CreateIndexes(CatalogExportProvider provider, IDocumentStore store, string database)
        {
            IndexCreation.CreateIndexes(provider, store.DatabaseCommands.ForDatabase(database), store.Conventions);
        }
    }
}