namespace ArcGisServerPermissionsProxy.Api.Raven.Models
{
    public class Config
    {
        public Config(string[] administrativeEmails)
        {
            AdministrativeEmails = administrativeEmails;
        }

        public string[] AdministrativeEmails { get; set; }
    }
}