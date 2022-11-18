using System;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication.OAuth.Json
{
    public class DeviceAuthorizationEndpointResponseJson
    {
        [JsonPropertyName("device_code")]
        // [JsonRequired]
        public string DeviceCode { get; set; }

        [JsonPropertyName("user_code")]
        // [JsonRequired]
        public string UserCode { get; set; }

        [JsonPropertyName("verification_uri")]
        // [JsonRequired]
        public Uri VerificationUri { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("interval")]
        public int? PollingInterval { get; set; }

        public OAuth2DeviceCodeResult ToResult()
        {
            return new OAuth2DeviceCodeResult(DeviceCode, UserCode, VerificationUri, PollingInterval, ExpiresIn);
        }
    }
}
