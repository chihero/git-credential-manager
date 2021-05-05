using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Authentication.OAuth;

namespace GitHub
{
    public interface IGitHubAuthentication : IDisposable
    {
        Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes);

        Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms);

        Task<OAuth2TokenResult> GetOAuthTokenAsync(Uri targetUri, IEnumerable<string> scopes, bool useBrowser);
    }

    public class DevicePrompt
    {
        private readonly CancellationTokenSource _cts;

        public DevicePrompt(CancellationTokenSource cts)
        {
            _cts = cts;
        }

        public CancellationToken CancellationToken => _cts.Token;

        public void Dismiss()
        {
            _cts.Cancel();
        }
    }

    public class AuthenticationPromptResult
    {
        public AuthenticationPromptResult(AuthenticationModes mode)
        {
            AuthenticationMode = mode;
        }

        public AuthenticationPromptResult(AuthenticationModes mode, ICredential credential)
            : this(mode)
        {
            Credential = credential;
        }

        public AuthenticationModes AuthenticationMode { get; }

        public ICredential Credential { get; set; }
    }

    [Flags]
    public enum AuthenticationModes
    {
        None    = 0,
        Basic   = 1,
        Browser = 1 << 1,
        Pat     = 1 << 2,
        Device  = 1 << 3,

        OAuth = Browser | Device,
        All   = Basic | Browser | Pat | Device
    }

    public class GitHubAuthentication : AuthenticationBase, IGitHubAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "github",
        };

        public GitHubAuthentication(ICommandContext context)
            : base(context) {}

        public async Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes)
        {
            ThrowIfUserInteractionDisabled();

            if (modes == AuthenticationModes.None)
            {
                throw new ArgumentException(@$"Must specify at least one {nameof(AuthenticationModes)}", nameof(modes));
            }

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                var promptArgs = new StringBuilder("prompt");
                if (modes == AuthenticationModes.All)
                {
                    promptArgs.Append(" --all");
                }
                else
                {
                    if ((modes & AuthenticationModes.Basic)   != 0) promptArgs.Append(" --basic");
                    if ((modes & AuthenticationModes.Browser) != 0) promptArgs.Append(" --browser");
                    if ((modes & AuthenticationModes.Device)  != 0) promptArgs.Append(" --device");
                    if ((modes & AuthenticationModes.Pat)     != 0) promptArgs.Append(" --pat");
                }
                if (!GitHubHostProvider.IsGitHubDotCom(targetUri)) promptArgs.AppendFormat(" --enterprise-url {0}", QuoteCmdArg(targetUri.ToString()));
                if (!string.IsNullOrWhiteSpace(userName)) promptArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));

                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, promptArgs.ToString(), null);

                if (!resultDict.TryGetValue("mode", out string responseMode))
                {
                    throw new Exception("Missing 'mode' in response");
                }

                switch (responseMode.ToLowerInvariant())
                {
                    case "pat":
                        if (!resultDict.TryGetValue("pat", out string pat))
                        {
                            throw new Exception("Missing 'pat' in response");
                        }

                        return new AuthenticationPromptResult(
                            AuthenticationModes.Pat, new GitCredential(userName, pat));

                    case "browser":
                        return new AuthenticationPromptResult(AuthenticationModes.Browser);

                    case "device":
                        return new AuthenticationPromptResult(AuthenticationModes.Device);

                    case "basic":
                        if (!resultDict.TryGetValue("username", out userName))
                        {
                            throw new Exception("Missing 'username' in response");
                        }

                        if (!resultDict.TryGetValue("password", out string password))
                        {
                            throw new Exception("Missing 'password' in response");
                        }

                        return new AuthenticationPromptResult(
                            AuthenticationModes.Basic, new GitCredential(userName, password));

                    default:
                        throw new Exception($"Unknown mode value in response '{responseMode}'");
                }
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                switch (modes)
                {
                    case AuthenticationModes.Basic:
                        Context.Terminal.WriteLine("Enter GitHub credentials for '{0}'...", targetUri);

                        if (string.IsNullOrWhiteSpace(userName))
                        {
                            userName = Context.Terminal.Prompt("Username");
                        }
                        else
                        {
                            Context.Terminal.WriteLine("Username: {0}", userName);
                        }

                        string password = Context.Terminal.PromptSecret("Password");

                        return new AuthenticationPromptResult(
                            AuthenticationModes.Basic, new GitCredential(userName, password));

                    case AuthenticationModes.Browser:
                        return new AuthenticationPromptResult(AuthenticationModes.Browser);

                    case AuthenticationModes.Device:
                        return new AuthenticationPromptResult(AuthenticationModes.Device);

                    case AuthenticationModes.Pat:
                        Context.Terminal.WriteLine("Enter GitHub personal access token for '{0}'...", targetUri);
                        string pat = Context.Terminal.PromptSecret("Token");
                        return new AuthenticationPromptResult(
                            AuthenticationModes.Pat, new GitCredential(userName, pat));

                    case AuthenticationModes.None:
                        throw new ArgumentOutOfRangeException(nameof(modes), @$"At least one {nameof(AuthenticationModes)} must be supplied");

                    default:
                        var menuTitle = $"Select an authentication method for '{targetUri}'";
                        var menu = new TerminalMenu(Context.Terminal, menuTitle);

                        TerminalMenuItem browserItem = null;
                        TerminalMenuItem deviceItem = null;
                        TerminalMenuItem basicItem = null;
                        TerminalMenuItem patItem = null;

                        // Only offer the web browser option in an interactive session
                        if (Context.SessionManager.IsDesktopSession)
                        {
                            if ((modes & AuthenticationModes.Browser) != 0) browserItem = menu.Add("Web browser");
                        }

                        if ((modes & AuthenticationModes.Device)  != 0) deviceItem = menu.Add("Device code");
                        if ((modes & AuthenticationModes.Pat)     != 0) patItem   = menu.Add("Personal access token");
                        if ((modes & AuthenticationModes.Basic)   != 0) basicItem = menu.Add("Username/password");

                        // Default to the 'first' choice in the menu
                        TerminalMenuItem choice = menu.Show(0);

                        if (choice == browserItem) goto case AuthenticationModes.Browser;
                        if (choice == deviceItem) goto case AuthenticationModes.Device;
                        if (choice == basicItem) goto case AuthenticationModes.Basic;
                        if (choice == patItem)   goto case AuthenticationModes.Pat;

                        throw new Exception();
                }
            }
        }

        public async Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms)
        {
            ThrowIfUserInteractionDisabled();

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                var args = new StringBuilder("2fa");
                if (isSms) args.Append(" --sms");

                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, args.ToString(), null);

                if (!resultDict.TryGetValue("code", out string authCode))
                {
                    throw new Exception("Missing 'code' in response");
                }

                return authCode;
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                Context.Terminal.WriteLine("Two-factor authentication is enabled and an authentication code is required.");

                if (isSms)
                {
                    Context.Terminal.WriteLine("An SMS containing the authentication code has been sent to your registered device.");
                }
                else
                {
                    Context.Terminal.WriteLine("Use your registered authentication app to generate an authentication code.");
                }

                return Context.Terminal.Prompt("Authentication code");
            }
        }

        public async Task<OAuth2TokenResult> GetOAuthTokenAsync(Uri targetUri, IEnumerable<string> scopes, bool useBrowser)
        {
            ThrowIfUserInteractionDisabled();

            var oauthClient = new GitHubOAuth2Client(HttpClient, Context.Settings, targetUri);

            if (useBrowser)
            {
                // Can only use the user's default browser if we have a desktop session
                if (!Context.SessionManager.IsDesktopSession)
                {
                    throw new InvalidOperationException("Cannot launch web browser without an GUI/interactive session.");
                }

                var browserOptions = new OAuth2WebBrowserOptions
                {
                    SuccessResponseHtml = GitHubResources.AuthenticationResponseSuccessHtml,
                    FailureResponseHtmlFormat = GitHubResources.AuthenticationResponseFailureHtmlFormat
                };
                var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);

                // Write message to the terminal (if any is attached) for some feedback that we're waiting for a web response
                Context.Terminal.WriteLine("info: please complete authentication in your browser...");

                OAuth2AuthorizationCodeResult authCodeResult = await oauthClient.GetAuthorizationCodeAsync(scopes, browser, CancellationToken.None);

                return await oauthClient.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);
            }
            else
            {
                OAuth2DeviceCodeResult deviceResult = await oauthClient.GetDeviceCodeAsync(scopes, CancellationToken.None);
                DevicePrompt devicePrompt = ShowDeviceCode(deviceResult.UserCode, deviceResult.VerificationUri);
                var result = await oauthClient.GetTokenByDeviceCodeAsync(deviceResult, devicePrompt.CancellationToken);
                devicePrompt.Dismiss();
                return result;
            }
        }

        private DevicePrompt ShowDeviceCode(string code, Uri verificationUri)
        {
            var cts = new CancellationTokenSource();

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                var args = new StringBuilder("device");
                args.AppendFormat(" {0}", QuoteCmdArg(code));
                args.AppendFormat(" {0}", QuoteCmdArg(verificationUri.ToString()));

                // Do not await.. we want to continue running whilst the helper is displaying the code.
                Task _ = InvokeHelperAsync(helperPath, args.ToString(), null, cts.Token)
                    .ContinueWith(t =>
                    {
                        // If the helper returns before the caller cancels then the dialog was closed
                        // so we should propagate any user cancellation request to the caller.
                         if (t.IsCanceled || (t.Result.TryGetValue("cancel", out string cancelStr) && cancelStr.IsTruthy()))
                         {
                             cts.Cancel();
                         }
                    }, cts.Token);
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                string deviceMessage =
                    $"To complete authentication please visit {verificationUri} and enter the following code:" +
                    Environment.NewLine +
                    code;

                Context.Terminal.WriteLine(deviceMessage);
            }

            return new DevicePrompt(cts);
        }

        private bool TryFindHelperExecutablePath(out string path)
        {
            return TryFindHelperExecutablePath(
                GitHubConstants.EnvironmentVariables.AuthenticationHelper,
                GitHubConstants.GitConfiguration.Credential.AuthenticationHelper,
                GitHubConstants.DefaultAuthenticationHelper,
                out path);
        }


        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ?? (_httpClient = Context.HttpClientFactory.CreateClient());

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
