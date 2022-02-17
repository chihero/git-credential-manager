using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager.UI.Controls;
using Microsoft.AzureRepos.UI.ViewModels;

namespace Microsoft.AzureRepos.UI.Views
{
    public class AccountPickerView : UserControl, IFocusable
    {
        private Button _continueButton;

        public AccountPickerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _continueButton = this.FindControl<Button>("continueButton");
        }

        public void SetFocus()
        {
            if (!(DataContext is AccountPickerViewModel vm))
            {
                return;
            }

            // TODO: set focus
        }
    }
}
