using AgrcPasswordManagement.Models.Account;

namespace ArcGisServerPermissionsProxy.Api.Models.Account
{
    public class Credentials : LoginCredentials
    {
        public Credentials(string email, string password, string application)
            : base(email, password, application)
        {
        }

        public string Name { get; set; }

        public string Agency { get; set; }
    }
}