using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Authentication.OpenIdConnect;
using GitCredentialManager.Authentication.OpenIdConnect.Json;
using GitCredentialManager.Interop.MacOS;
using GitCredentialManager.Interop.Posix;
using Xunit;

namespace GitCredentialManager.Tests.Authentication
{
    public class OidcClientTests
    {
        [Fact]
        public async Task OidcClient_GetConfigurationAsync()
        {
            string clientId = "d735b71b-9eee-4a4f-ad23-421660877ba6"; // GCM
            var redirectUri = new Uri("http://localhost");
            var authority = new Uri("https://login.microsoftonline.com/common/v2.0"); // MS common authority (v2)
            var scopes = new[]
            {
                "499b84ac-1321-427f-aa17-267ca6975798/vso.code_full", // Azure DevOps repository read/write
                "offline_access", // Refresh Token
                "openid", "profile", "email" // ID Token with name, username and email
            };

            var fs = new MacOSFileSystem();
            var env = new PosixEnvironment(fs);
            var httpClient = new HttpClient();
            var ct = CancellationToken.None;
            var browserOptions = new OAuth2WebBrowserOptions();
            var browser = new OAuth2SystemWebBrowser(env, browserOptions);

            IOidcClient oidcClient = new OidcClient(httpClient, authority);
            OidcConfiguration oidcConfig = await oidcClient.GetConfigurationAsync();

            // var endpoints = new OAuth2ServerEndpoints(
            //     oidcConfig.AuthorizationEndpoint,
            //     oidcConfig.TokenEndpoint)
            // {
            //     DeviceAuthorizationEndpoint = oidcConfig.DeviceAuthorizationEndpoint
            // };
            //
            // IOAuth2Client oauthClient = new OAuth2Client(httpClient, endpoints, clientId, redirectUri);
            //
            // OAuth2AuthorizationCodeResult authCode = await oauthClient.GetAuthorizationCodeAsync(scopes, browser, ct);
            // OAuth2TokenResult result = await oauthClient.GetTokenByAuthorizationCodeAsync(authCode, ct);
        }
    }
}
