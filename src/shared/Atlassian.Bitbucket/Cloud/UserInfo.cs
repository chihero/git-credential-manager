using System;
using System.Text.Json.Serialization;

namespace Atlassian.Bitbucket.Cloud
{
    public class UserInfo : IUserInfo
    {
        [JsonPropertyName("has_2fa_enabled")]
        public bool IsTwoFactorAuthenticationEnabled { get; set; }

        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("account_id")]
        public string AccountId { get; set; }

        [JsonPropertyName("uuid")]
        public Guid Uuid { get; set; }
    }
}
