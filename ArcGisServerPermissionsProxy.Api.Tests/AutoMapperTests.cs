using System.Collections.Generic;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionProxy.Domain.ViewModels;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests
{
    [TestFixture]
    public class AutoMapperTests
    {
        [Test]
        public void CanMapListOfUsers()
        {
            AutoMapperConfig.RegisterMaps();

            var list = new[]
            {
                new User("Not Approved", " but Active", "notApprovedActiveUser@test.com",
                    "AGENCY", "abc", "SALT", null,
                    null, null, null, null),

                new User("Approved and", "Active", "approvedActiveUser@test.com", "AGENCY",
                    "abc", "SALT", null,
                    "admin", null, null, null)
                {
                    Active = false,
                    Approved = true
                },
                new User("Not approved", "or active", "notApprovedNotActiveUser@test.com",
                    "AGENCY", "abc", "SALT", null,
                    null, null, null, null)
                {
                    Active = false
                }
            };

            var userModels = AutoMapper.Mapper.Map<IList<User>, IList<UserViewModel>>(list);

            Assert.That(userModels, Is.Not.Null);
        }
    }
}