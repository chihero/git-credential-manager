using System.Text.Json.Serialization;
using GitCredentialManager.Authentication.OAuth.Json;

namespace Atlassian.Bitbucket
{
        public class BitbucketTokenEndpointResponseJson : TokenEndpointResponseJson
        {
            // Bitbucket uses "scopes" for the scopes property name rather than the standard "scope" name
            [JsonPropertyName("scopes")]
            public override string Scope { get; set; }
        }
}
