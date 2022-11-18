using System;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication.OAuth.Json
{
    public class TokenEndpointResponseJson
    {
        [JsonPropertyName("access_token")]
        // [JsonRequired]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        // [JsonRequired]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public virtual string Scope { get; set; }

        public OAuth2TokenResult ToResult()
        {
            return new OAuth2TokenResult(AccessToken, TokenType)
            {
                ExpiresIn = ExpiresIn.ToTimeSpan(TimeUnit.Seconds),
                RefreshToken = RefreshToken,
                Scopes = Scope?.Split(' ')
            };
        }
    }
}
