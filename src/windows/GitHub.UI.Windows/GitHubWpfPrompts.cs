// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using GitHub.UI.Login;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication.OAuth;
using Microsoft.Git.CredentialManager.UI;

namespace GitHub.UI
{
    public class GitHubWpfPrompts : PromptsBase , IGitHubPrompts
    {
        private readonly IGui _gui;

        public GitHubWpfPrompts(ISettings settings, IGui gui) : base(settings)
        {
            EnsureArgument.NotNull(gui, nameof(gui));

            _gui = gui;
        }

        public Task<AuthenticationPromptResult> ShowAuthenticationPromptAsync(
            Uri targetUri, string userName, AuthenticationModes modes)
        {
            ThrowIfUserInteractionDisabled();

            bool showBasic = (modes & AuthenticationModes.Basic) != 0;
            bool showOAuth = (modes & AuthenticationModes.OAuth) != 0;
            bool showPat   = (modes & AuthenticationModes.Pat)   != 0;

            var viewModel = new LoginCredentialsViewModel(showBasic, showOAuth, showPat)
            {
                UsernameOrEmail = userName
            };

            if (!GitHubHostProvider.IsGitHubDotCom(targetUri))
            {
                viewModel.GitHubEnterpriseUrl = targetUri.ToString();
            }

            bool valid = _gui.ShowDialogWindow(viewModel, () => new LoginCredentialsView());

            switch (viewModel.SelectedAuthType)
            {
                case CredentialPromptResult.BasicAuthentication:
                    if (valid)
                    {
                        return Task.FromResult(new AuthenticationPromptResult(
                            AuthenticationModes.Basic,
                            new GitCredential(viewModel.UsernameOrEmail, viewModel.Password.ToUnsecureString()))
                        );
                    }
                    break;

                case CredentialPromptResult.OAuthAuthentication:
                    return Task.FromResult(new AuthenticationPromptResult(AuthenticationModes.OAuth));

                case CredentialPromptResult.PersonalAccessToken:
                    if (valid)
                    {
                        return Task.FromResult(new AuthenticationPromptResult(
                            AuthenticationModes.Pat,
                            new GitCredential(userName, viewModel.Token.ToUnsecureString()))
                        );
                    }
                    break;
            }

            throw new Exception("User cancelled dialog");
        }


        public Task<TwoFactorCodePromptResult> ShowTwoFactorCodePromptAsync(Uri targetUri, bool isSms)
        {
            ThrowIfUserInteractionDisabled();

            var viewModel = new Login2FaViewModel(isSms ? TwoFactorType.Sms : TwoFactorType.AuthenticatorApp);

            bool valid = _gui.ShowDialogWindow(viewModel, () => new Login2FaView());

            var authenticationCode = valid ? viewModel.AuthenticationCode : null;

            return Task.FromResult(new TwoFactorCodePromptResult(authenticationCode));
        }

        public Task ShowDeviceCodeAsync(OAuth2DeviceCodeResult deviceCodeResult)
        {
            ThrowIfUserInteractionDisabled();

            var viewModel = new LoginDeviceViewModel(deviceCodeResult.UserCode, deviceCodeResult.VerificationUri);

            bool valid = _gui.ShowDialogWindow(viewModel, () => new LoginDeviceView());

            return Task.CompletedTask;
        }
    }
}
