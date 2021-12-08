using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitCredentialManager;

namespace Microsoft.AzureRepos
{
    public interface IAzureDevOpsRestApi : IDisposable
    {
        Task<string> GetAuthorityAsync(Uri organizationUri);
    }

    public class AzureDevOpsRestApi : IAzureDevOpsRestApi
    {
        private readonly ICommandContext _context;

        public AzureDevOpsRestApi(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public async Task<string> GetAuthorityAsync(Uri organizationUri)
        {
            EnsureArgument.AbsoluteUri(organizationUri, nameof(organizationUri));

            Uri authorityBase = GetAuthorityBaseUri();
            var commonAuthority = new Uri(authorityBase, "common");

            // We should be using "/common" or "/consumer" as the authority for MSA but since
            // Azure DevOps uses MSA pass-through (an internal hack to support MSA and AAD
            // accounts in the same auth stack), which actually need to consult the "/organizations"
            // authority instead.
            var msaAuthority = new Uri(authorityBase, "organizations");

            _context.Trace.WriteLine($"HTTP: HEAD {organizationUri}");
            using (HttpResponseMessage response = await HttpClient.HeadAsync(organizationUri))
            {
                _context.Trace.WriteLine("HTTP: Response code ignored.");
                _context.Trace.WriteLine("Inspecting headers...");

                // Check WWW-Authenticate headers first; we prefer these
                foreach (var header in response.Headers.WwwAuthenticate)
                {
                    if (TryGetAuthorityFromHeader(header, out string authority))
                    {
                        _context.Trace.WriteLine(
                            $"Found WWW-Authenticate header with Bearer authority '{authority}'.");
                        return authority;
                    }
                }

                // We didn't find a bearer WWW-Auth header; check for the X-VSS-ResourceTenant header
                foreach (var header in response.Headers)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(header.Key, AzureDevOpsConstants.VssResourceTenantHeader))
                    {
                        string[] tenantIds = header.Value.ToArray();
                        Guid guid;

                        // Take the first tenant ID that isn't an empty GUID
                        var tenantId = tenantIds.FirstOrDefault(x => Guid.TryParse(x, out guid) && guid != Guid.Empty);
                        if (tenantId != null)
                        {
                            _context.Trace.WriteLine($"Found {AzureDevOpsConstants.VssResourceTenantHeader} header with AAD tenant ID '{tenantId}'.");
                            return new Uri(authorityBase, tenantId).ToString();
                        }

                        // If we have exactly one empty GUID then this is a MSA backed organization
                        if (tenantIds.Length == 1 && Guid.TryParse(tenantIds[0], out guid) && guid == Guid.Empty)
                        {
                            _context.Trace.WriteLine($"Found {AzureDevOpsConstants.VssResourceTenantHeader} header with MSA tenant ID (empty GUID).");
                            return msaAuthority.ToString();
                        }
                    }
                }
            }

            // Use the common authority if we can't determine a specific one
            _context.Trace.WriteLine($"Unable to determine AAD/MSA tenant - falling back to common authority");
            return commonAuthority.ToString();
        }

        private Uri GetAuthorityBaseUri()
        {
            // Check for developer override value
            if (_context.Settings.TryGetSetting(
                    AzureDevOpsConstants.EnvironmentVariables.DevAadAuthorityBaseUri,
                    Constants.GitConfiguration.Credential.SectionName, AzureDevOpsConstants.GitConfiguration.Credential.DevAadAuthorityBaseUri,
                    out string redirectUriStr) &&
                Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri authorityBase))
            {
                return authorityBase;
            }

            return new Uri(AzureDevOpsConstants.AadAuthorityBaseUrl);
        }

        private const RegexOptions CommonRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

        private HttpClient _httpClient;

        private HttpClient HttpClient
        {
            get
            {
                if (_httpClient is null)
                {
                    _httpClient = _context.HttpClientFactory.CreateClient();

                    // Configure the HTTP client with standard headers for Azure Repos API calls
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.Http.MimeTypeJson));
                }

                return _httpClient;
            }
        }

        /// <summary>
        /// Attempt to extract the authority from a Authorization Bearer header.
        /// </summary>
        /// <remarks>This method has internal visibility for testing purposes only.</remarks>
        /// <param name="header">Request header</param>
        /// <param name="authority">Value of authorization authority, or null if not found.</param>
        /// <returns>True if an authority was found in the header, false otherwise.</returns>
        internal static bool TryGetAuthorityFromHeader(AuthenticationHeaderValue header, out string authority)
        {
            // We're looking for a "Bearer" scheme header
            if (!(header is null) &&
                StringComparer.OrdinalIgnoreCase.Equals(header.Scheme, Constants.Http.WwwAuthenticateBearerScheme) &&
                header.Parameter is string headerValue)
            {
                Match match = Regex.Match(headerValue, @"^authorization_uri=(?'authority'.+)$", CommonRegexOptions);

                if (match.Success)
                {
                    authority = match.Groups["authority"].Value;
                    return true;
                }
            }

            authority = null;
            return false;
        }

        /// <summary>
        /// Parse the input JSON string looking for the first string field with the specified name.
        /// </summary>
        /// <remarks>This method has internal visibility for testing purposes only.</remarks>
        /// <param name="json">JSON string</param>
        /// <param name="fieldName">Name of field to locate.</param>
        /// <param name="value">Value of first found field, or null if no such field was found.</param>
        /// <returns>True if a field and value was found, false otherwise.</returns>
        internal static bool TryGetFirstJsonStringField(string json, string fieldName, out string value)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                // Find the '"<field>" : "<value>"' portion of the JSON content
                string escapedFieldName = Regex.Escape(fieldName);
                string pattern = $"\"{escapedFieldName}\"\\s*\\:\\s*\"(?'value'[^\"]+)\"";
                Match match = Regex.Match(json, pattern, CommonRegexOptions);
                if (match.Success)
                {
                    value = match.Groups["value"].Value;
                    return true;
                }

            }

            value = null;
            return false;
        }

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion
    }
}
