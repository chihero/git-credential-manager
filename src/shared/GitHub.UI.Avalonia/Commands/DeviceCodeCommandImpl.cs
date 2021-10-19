using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitHub.UI.ViewModels;
using GitHub.UI.Views;

namespace GitHub.UI.Commands
{
    public class DeviceCodeCommandImpl : DeviceCodeCommand
    {
        public DeviceCodeCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(DeviceCodeViewModel viewModel, CancellationToken ct)
        {
            return AvaloniaUi.ShowViewAsync<DeviceCodeView>(viewModel, GetParentHandle(), ct);
        }
    }
}
