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

            if (user.AccessRules == null)
            {
                return;
            }

            AccessRules = user.AccessRules;
        }

        [JsonProperty]
        protected User.UserAccessRules AccessRules { get; set; }

        [JsonProperty]
        protected string Email { get; private set; }

        [JsonProperty]
        protected string First { get; set; }
        
        [JsonProperty]
        protected string Last { get; set; }

        [JsonProperty]
        protected string Agency { get; set; }

        public bool ShouldSerializeAccessRules()
        {
            return AccessRules.StartDate > 0;
        }
    }

}