using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Raven.Configuration
{
    public interface IDatabaseExists
    {
        void Ensure(IDocumentStore database, string name);
    }
}