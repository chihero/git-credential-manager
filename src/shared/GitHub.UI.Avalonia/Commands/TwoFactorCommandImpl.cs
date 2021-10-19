using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitHub.UI.ViewModels;
using GitHub.UI.Views;

namespace GitHub.UI.Commands
{
    public class TwoFactorCommandImpl : TwoFactorCommand
    {
        public TwoFactorCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(TwoFactorViewModel viewModel, CancellationToken ct)
        {
            return AvaloniaUi.ShowViewAsync<TwoFactorView>(viewModel, GetParentHandle(), ct);
        }
    }
}
