using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Raven.Configuration
{
    public class DatabaseExists : IDatabaseExists
    {
        public void Ensure(IDocumentStore database, string name)
        {
            database.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists(name);
        }
    }
}