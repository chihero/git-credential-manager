using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitLab.UI.ViewModels;
using GitLab.UI.Views;
using GitCredentialManager;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Interop.MacOS;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.Controls;

namespace GitLab.UI.Controls
{
    public class TesterWindow : Window
    {
        private readonly IEnvironment _environment;

        public TesterWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            if (OperatingSystem.IsWindows())
            {
                _environment = new WindowsEnvironment(new WindowsFileSystem());
            }
            else
            {
                IFileSystem fs;
                if (OperatingSystem.IsMacOS())
                {
                    fs = new MacOSFileSystem();
                }
                else if (OperatingSystem.IsLinux())
                {
                    fs = new LinuxFileSystem();
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }

                _environment = new PosixEnvironment(fs);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ShowCredentials(object sender, RoutedEventArgs e)
        {
            var vm = new CredentialsViewModel(_environment)
            {
                ShowBrowserLogin = this.FindControl<CheckBox>("useBrowser").IsChecked ?? false,
                ShowTokenLogin = this.FindControl<CheckBox>("usePat").IsChecked ?? false,
                ShowBasicLogin = this.FindControl<CheckBox>("useBasic").IsChecked ?? false,
                Url = this.FindControl<TextBox>("instanceUrl").Text,
                UserName = this.FindControl<TextBox>("username").Text
            };
            var view = new CredentialsView();
            var window = new DialogWindow(view) {DataContext = vm};
            window.ShowDialog(this);
        }
    }
}
