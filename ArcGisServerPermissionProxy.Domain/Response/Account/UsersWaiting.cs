using ArcGisServerPermissionProxy.Domain.Database;
using Newtonsoft.Json;

namespace ArcGisServerPermissionProxy.Domain.Response.Account
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UsersWaiting
    {
        public UsersWaiting(User user)
        {
            First = user.First;
            Last = user.Last;
            Agency = user.Agency;
            Email = user.Email;
        }

        [JsonProperty]
        protected string Email { get; private set; }

        [JsonProperty]
        protected string First { get; set; }
        
        [JsonProperty]
        protected string Last { get; set; }

        [JsonProperty]
        protected string Agency { get; set; }
    }

}