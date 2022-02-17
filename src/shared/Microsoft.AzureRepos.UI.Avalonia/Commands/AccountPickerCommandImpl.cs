using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.UI;
using Microsoft.AzureRepos.UI.ViewModels;
using Microsoft.AzureRepos.UI.Views;

namespace Microsoft.AzureRepos.UI.Commands
{
    public class AccountPickerCommandImpl : AccountPickerCommand
    {
        public AccountPickerCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(AccountPickerViewModel viewModel, CancellationToken ct)
        {
            return AvaloniaUi.ShowViewAsync<AccountPickerView>(viewModel, GetParentHandle(), ct);
        }
    }
}
