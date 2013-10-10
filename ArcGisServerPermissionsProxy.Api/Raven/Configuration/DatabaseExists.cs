using Raven.Client;
using Raven.Client.Extensions;

namespace ArcGisServerPermissionsProxy.Api.Raven.Configuration
{
    public class DatabaseExists : IDatabaseExists
    {
        public void Esure(IDocumentStore database, string name)
        {
            database.DatabaseCommands.EnsureDatabaseExists(name);
        }
    }
}