using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication.OAuth;

namespace GitHub
{
    public interface IGitHubPrompts
    {
        Task<AuthenticationPromptResult> ShowAuthenticationPromptAsync(Uri targetUri, string userName, AuthenticationModes modes);

        Task<TwoFactorCodePromptResult> ShowTwoFactorCodePromptAsync(Uri targetUri, bool isSms);

        Task ShowDeviceCodeAsync(OAuth2DeviceCodeResult deviceCodeResult);
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

    public class TwoFactorCodePromptResult
    {
        public TwoFactorCodePromptResult(string code)
        {
            AuthenticationCode = code;
        }

        public string AuthenticationCode { get; }
    }

    public class GitHubTerminalPrompts : TerminalPrompts, IGitHubPrompts
    {
        public GitHubTerminalPrompts(ICommandContext context)
            : base(context.Settings, context.Terminal) { }

        public GitHubTerminalPrompts(ISettings settings, ITerminal terminal)
            : base(settings, terminal) { }

        public Task<AuthenticationPromptResult> ShowAuthenticationPromptAsync(
            Uri targetUri, string userName, AuthenticationModes modes)
        {
            ThrowIfTerminalPromptsDisabled();

            switch (modes)
            {
                case AuthenticationModes.Basic:
                    Terminal.WriteLine("Enter GitHub credentials for '{0}'...", targetUri);

                    if (string.IsNullOrWhiteSpace(userName))
                    {
                        userName = Terminal.Prompt("Username");
                    }
                    else
                    {
                        Terminal.WriteLine("Username: {0}", userName);
                    }

                    string password = Terminal.PromptSecret("Password");

                    return Task.FromResult(new AuthenticationPromptResult(
                        AuthenticationModes.Basic, new GitCredential(userName, password)));

                case AuthenticationModes.OAuth:
                    return Task.FromResult(new AuthenticationPromptResult(AuthenticationModes.OAuth));

                case AuthenticationModes.Pat:
                    Terminal.WriteLine("Enter GitHub personal access token for '{0}'...", targetUri);
                    string pat = Terminal.PromptSecret("Token");
                    return Task.FromResult(new AuthenticationPromptResult(
                        AuthenticationModes.Pat, new GitCredential(userName, pat)));

                case AuthenticationModes.None:
                    throw new ArgumentOutOfRangeException(nameof(modes), @$"At least one {nameof(AuthenticationModes)} must be supplied");

                default:
                    var menuTitle = $"Select an authentication method for '{targetUri}'";
                    var menu = new TerminalMenu(Terminal, menuTitle);

                    TerminalMenuItem oauthItem = null;
                    TerminalMenuItem basicItem = null;
                    TerminalMenuItem patItem = null;

                    if ((modes & AuthenticationModes.OAuth) != 0) oauthItem = menu.Add("Web browser");
                    if ((modes & AuthenticationModes.Pat)   != 0) patItem   = menu.Add("Personal access token");
                    if ((modes & AuthenticationModes.Basic) != 0) basicItem = menu.Add("Username/password");

                    // Default to the 'first' choice in the menu
                    TerminalMenuItem choice = menu.Show(0);

                    if (choice == oauthItem) goto case AuthenticationModes.OAuth;
                    if (choice == basicItem) goto case AuthenticationModes.Basic;
                    if (choice == patItem)   goto case AuthenticationModes.Pat;

                    throw new Exception();
            }
        }

        public Task<TwoFactorCodePromptResult> ShowTwoFactorCodePromptAsync(Uri targetUri, bool isSms)
        {
            ThrowIfTerminalPromptsDisabled();

            Terminal.WriteLine("Two-factor authentication is enabled and an authentication code is required.");

            if (isSms)
            {
                Terminal.WriteLine("An SMS containing the authentication code has been sent to your registered device.");
            }
            else
            {
                Terminal.WriteLine("Use your registered authentication app to generate an authentication code.");
            }

            string code = Terminal.Prompt("Authentication code");

            return Task.FromResult(new TwoFactorCodePromptResult(code));
        }

        public Task ShowDeviceCodeAsync(OAuth2DeviceCodeResult deviceCodeResult)
        {
            string deviceMessage = $"To complete authentication please visit {deviceCodeResult.VerificationUri} and enter the following code:" +
                                   Environment.NewLine +
                                   deviceCodeResult.UserCode;
            Terminal.WriteLine(deviceMessage);
            return Task.CompletedTask;
        }
    }
}
