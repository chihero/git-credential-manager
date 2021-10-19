using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitHub.UI.ViewModels;
using GitHub.UI.Views;

namespace GitHub.UI.Commands
{
    public class CredentialsCommandImpl : CredentialsCommand
    {
        public CredentialsCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(CredentialsViewModel viewModel, CancellationToken ct)
        {
            return Gui.ShowDialogWindow(viewModel, () => new CredentialsView(), GetParentHandle());
        }
    }
}
