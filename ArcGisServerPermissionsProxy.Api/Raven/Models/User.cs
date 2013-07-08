namespace ArcGisServerPermissionsProxy.Api.Raven.Models
{
    public class User
    {
        public User(string email, string password, string salt, string application, string role)
        {
            Email = email;
            Password = password;
            Salt = salt;
            Application = application;
            Role = role;
        }

        public string Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Application { get; set; }
        public string Role { get; set; }
        public string Salt { get; set; }
    }
}