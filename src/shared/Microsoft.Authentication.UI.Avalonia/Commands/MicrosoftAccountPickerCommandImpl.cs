using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.Commands;
using GitCredentialManager.UI.ViewModels;
using Microsoft.Identity.GitCredentialManager.Views;

namespace Microsoft.Identity.GitCredentialManager.Commands
{
    public class MicrosoftAccountPickerCommandImpl : MicrosoftAccountPickerCommand
    {
        public MicrosoftAccountPickerCommandImpl(ICommandContext context)
            : base(context) { }

        protected override Task ShowAsync(MicrosoftAccountPickerViewModel viewModel, CancellationToken ct)
        {
            return AvaloniaUi.ShowViewAsync<MicrosoftAccountPickerView>(viewModel, GetParentHandle(), ct);
        }
    }
}
