using System;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.Controls;
using Atlassian.Bitbucket.UI.ViewModels;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace Atlassian.Bitbucket.UI
{
    public class BitbucketWpfPrompts : PromptsBase, IBitbucketPrompts
    {
        private readonly IGui _gui;

        public BitbucketWpfPrompts(ISettings settings, IGui gui) : base(settings)
        {
            EnsureArgument.NotNull(gui, nameof(gui));

            _gui = gui;
        }

        public Task<BasicPromptResult> ShowBasicPromptAsync(Uri targetUri, string userName)
        {
            ThrowIfUserInteractionDisabled();

            // If there is a user in the remote URL then populate the UI with it.
            var credentialViewModel = new CredentialsViewModel(userName);

            bool credentialValid = _gui.ShowDialogWindow(credentialViewModel, () => new CredentialsControl());

            ICredential credential = credentialValid
                ? new GitCredential(credentialViewModel.Login, credentialViewModel.Password.ToUnsecureString())
                : null;

            return Task.FromResult(new BasicPromptResult{Credential = credential});
        }

        public Task<OAuthPromptResult> ShowOAuthPromptAsync()
        {
            ThrowIfUserInteractionDisabled();

            var oauthViewModel = new OAuthViewModel();

            bool useOAuth = _gui.ShowDialogWindow(oauthViewModel, () => new OAuthControl());

            return Task.FromResult(new OAuthPromptResult{Continue = useOAuth});
        }
    }
}
