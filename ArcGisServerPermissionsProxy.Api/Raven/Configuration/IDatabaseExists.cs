using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Raven.Configuration
{
    public interface IDatabaseExists
    {
        void Esure(IDocumentStore database, string name);
    }
}