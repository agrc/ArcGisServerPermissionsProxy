namespace ArcGisServerPermissionsProxy.Api.Raven.Models
{
    public class Application
    {
        public Application(string name, string password)
        {
            Name = name;
            Password = password;
        }

        public string Name { get; set; }
        public string Password { get; set; }
    }
}