using System.Linq;
using ArcGisServerPermissionProxy.Domain.Database;
using Raven.Client.Indexes;

namespace ArcGisServerPermissionsProxy.Api.Raven.Indexes
{
    public class ApplicationByNameIndex : AbstractIndexCreationTask<Application>
    {
        public ApplicationByNameIndex()
        {
            Map = apps => from app in apps
                          select new
                              {
                                  app.Name
                              };
        }
    }
}