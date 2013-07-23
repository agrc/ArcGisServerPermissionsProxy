namespace ArcGisServerPermissionsProxy.Api.Services
{
    public class MockValidationService : IValidationService
    {
        public bool IsValid(string application)
        {
            return true;
        }
    }
}