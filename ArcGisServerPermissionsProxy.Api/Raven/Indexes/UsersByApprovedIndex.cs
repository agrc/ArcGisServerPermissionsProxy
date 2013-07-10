using System.Linq;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using Raven.Client.Indexes;

namespace ArcGisServerPermissionsProxy.Api.Raven.Indexes
{
    public class UsersByApprovedIndex : AbstractIndexCreationTask<User>
    {
        public UsersByApprovedIndex()
        {
            Map = users => from user in users
                           select new
                               {
                                   user.Approved,
                                   user.Active
                               };
        }
    }
}