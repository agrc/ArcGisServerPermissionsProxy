namespace ArcGisServerPermissionsProxy.Api.Models.Account
{
    public class AdminCredentials
    {
        public AdminCredentials(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username { get; set; }
        public string Password { get; set; }
    }
}