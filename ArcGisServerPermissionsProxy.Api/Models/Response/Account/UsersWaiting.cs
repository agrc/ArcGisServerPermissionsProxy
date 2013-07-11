using ArcGisServerPermissionsProxy.Api.Raven.Models;
using Newtonsoft.Json;

namespace ArcGisServerPermissionsProxy.Api.Models.Response.Account
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UsersWaiting
    {
        public UsersWaiting(User user)
        {
            Name = user.Name;
            Agency = user.Agency;
            Email = user.Email;
        }

        [JsonProperty(PropertyName = "Email")]
        protected string Email { get; private set; }

        [JsonProperty(PropertyName = "name")]
        protected string Name { get; set; }

        [JsonProperty(PropertyName = "agency")]
        protected string Agency { get; set; }
    }
}