namespace ArcGisServerPermissionsProxy.Api.Raven.Models
{
    public class Application
    {
        private string _name;

        public Application(string name, string password)
        {
            Name = name;
            Password = password;
        }

        public string Name
        {
            get { return _name; }
            private set
            {
                if (value == null || value.ToLowerInvariant() == "system" || string.IsNullOrEmpty(value))
                    _name = null;
                else
                {
                    _name = value;
                }
            }
        }
        public string Password { get; set; }
    }
}