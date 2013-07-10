using AgrcPasswordManagement.Models.Account;

namespace ArcGisServerPermissionsProxy.Api.Models.Account
{
    public class Credentials : LoginCredentials
    {
        public Credentials(string email, string password, string applicationName, string roleName, string database)
            : base(email, password, roleName, applicationName)
        {
            database = database.ToLowerInvariant();

            if (database.Contains("system"))
                database = null;

            Database = database;
        }

        public string Database { get; set; }

        public string Name { get; set; }

        public string Agency { get; set; }
    }
}