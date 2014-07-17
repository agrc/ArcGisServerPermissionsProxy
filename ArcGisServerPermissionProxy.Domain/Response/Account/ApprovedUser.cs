using ArcGisServerPermissionProxy.Domain.Database;
using Newtonsoft.Json;

namespace ArcGisServerPermissionProxy.Domain.Response.Account {

    public class ApprovedUser : UsersWaiting {
        public ApprovedUser(User user) : base(user)
        {
            Role = user.Role;
            AccessRules = user.AccessRules;
            LastLogin = user.LastLogin;
        }

        [JsonProperty]
        public long LastLogin { get; set; }
        
        [JsonProperty]
        public User.UserAccessRules AccessRules { get; set; }

        [JsonProperty]
        public string Role { get; set; }

        public bool ShouldSerializeAccessRules()
        {
            return AccessRules.StartDate > 0;
        }
    }

}