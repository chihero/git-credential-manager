using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitCredentialManager;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Interop.MacOS;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.Controls;
using Microsoft.AzureRepos.UI.ViewModels;
using Microsoft.AzureRepos.UI.Views;

namespace Microsoft.AzureRepos.UI.Controls
{
    public class TesterWindow : Window
    {
        public TesterWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ShowAccountPicker(object sender, RoutedEventArgs e)
        {
            var vm = new AccountPickerViewModel();
            var view = new AccountPickerView();
            var window = new DialogWindow(view) { DataContext = vm };
            window.ShowDialog(this);
        }
    }
}
