namespace ArcGisServerPermissionsProxy.Api.Services
{
    public class ValidationService : IValidationService
    {
        public bool IsValid(string application)
        {
            return !string.IsNullOrEmpty(application);
        }
    }
}