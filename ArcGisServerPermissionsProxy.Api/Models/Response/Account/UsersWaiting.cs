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

        [JsonProperty]
        protected string Email { get; private set; }

        [JsonProperty]
        protected string Name { get; set; }

        [JsonProperty]
        protected string Agency { get; set; }
    }
}