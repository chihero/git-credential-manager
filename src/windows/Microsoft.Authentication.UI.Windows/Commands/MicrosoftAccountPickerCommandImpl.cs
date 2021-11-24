using System.Threading;
using System.Threading.Tasks;
using Microsoft.Authentication.UI.Views;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.Commands;
using GitCredentialManager.UI.ViewModels;

namespace Microsoft.Authentication.UI.Commands
{
    public class MicrosoftAccountPickerCommandImpl : MicrosoftAccountPickerCommand
    {
        public MicrosoftAccountPickerCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(MicrosoftAccountPickerViewModel viewModel, CancellationToken ct)
        {
            return Gui.ShowDialogWindow(viewModel, () => new MicrosoftAccountPickerView(), GetParentHandle());
        }
    }
}
