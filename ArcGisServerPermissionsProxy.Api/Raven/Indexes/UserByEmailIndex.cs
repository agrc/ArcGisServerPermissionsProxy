﻿using System.Linq;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using Raven.Client.Indexes;

namespace ArcGisServerPermissionsProxy.Api.Raven.Indexes
{
    public class UserByEmailIndex : AbstractIndexCreationTask<User>
    {
        public UserByEmailIndex()
        {
            Map = users => from user in users
                           select new
                               {
                                   user.Email,
                                   user.Application
                               };
        }
    }
}