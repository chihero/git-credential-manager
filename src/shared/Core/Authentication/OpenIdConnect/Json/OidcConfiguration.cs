using System;
using Newtonsoft.Json;

namespace GitCredentialManager.Authentication.OpenIdConnect.Json
{
    public class OidcConfiguration
    {
        [JsonProperty("issuer", Required = Required.Always)]
        public string Issuer { get; set; }

        [JsonProperty("authorization_endpoint", Required = Required.Always)]
        public Uri AuthorizationEndpoint { get; set; }

        [JsonProperty("token_endpoint", Required = Required.Always)]
        public Uri TokenEndpoint { get; set; }

        [JsonProperty("userinfo_endpoint", Required = Required.Always)]
        public Uri UserInfoEndpoint { get; set; }

        [JsonProperty("registration_endpoint")]
        public Uri RegistrationEndpoint { get; set; }

        [JsonProperty("device_authorization_endpoint")]
        public Uri DeviceAuthorizationEndpoint { get; set; }
    }
}
