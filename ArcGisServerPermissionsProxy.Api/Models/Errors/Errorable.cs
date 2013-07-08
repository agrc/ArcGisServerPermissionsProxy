using ArcGisServerPermissionsProxy.Api.Models.ArcGIS;

namespace ArcGisServerPermissionsProxy.Api.Models.Errors
{
    public class Errorable
    {
        public ArcGisServerError Error { get; set; }

        public bool Successful
        {
            get { return Error == null || string.IsNullOrEmpty(Error.Message); }
        }
    }
}