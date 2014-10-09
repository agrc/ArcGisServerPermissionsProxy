using System.Linq;
using ArcGisServerPermissionProxy.Domain.Database;
using Raven.Client.Indexes;

namespace ArcGisServerPermissionsProxy.Api.Raven.Indexes {

    public class UserByIdIndex : AbstractIndexCreationTask<User>
    {
        public UserByIdIndex()
        {
            Map = users => from user in users
                           select new
                               {
                                   user.UserId
                               };
        }
    }

}