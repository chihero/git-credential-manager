using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.OpenIdConnect.Json;
using Newtonsoft.Json;

namespace GitCredentialManager.Authentication.OpenIdConnect
{
    /// <summary>
    /// Represents an OpenID Connect client.
    /// </summary>
    public interface IOidcClient
    {
        /// <summary>
        /// Get the OpenID Connect configuration information from the well-known endpoint for the authority.
        /// </summary>
        /// <returns>OpenID Connect configuration.</returns>
        Task<OidcConfiguration> GetConfigurationAsync();
    }

    public class OidcClient : IOidcClient
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _authority;

        public OidcClient(HttpClient httpClient, Uri authority)
        {
            EnsureArgument.NotNull(httpClient, nameof(httpClient));

            _httpClient = httpClient;
            _authority = authority;
        }

        public async Task<OidcConfiguration> GetConfigurationAsync()
        {
            // Append "/.well-known/openid-configuration" to the issuer URI.
            // Note that we cannot use new Uri(baseUri, relativePath) because if the relativePath
            // component starts with a leading '/' then the path is relative to the root authority
            // of the baseUri - we want a simple
            var sb = new StringBuilder(_authority.ToString());
            if (sb[sb.Length - 1] != '/') sb.Append('/');
            sb.Append(".well-known/openid-configuration");

            var discoveryUri = new Uri(sb.ToString());

            using (HttpResponseMessage response = await _httpClient.GetAsync(discoveryUri))
            {
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<OidcConfiguration>(json);
            }
        }
    }
}
