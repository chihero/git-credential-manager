using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitCredentialManager.UI.Controls;
using GitCredentialManager.UI.ViewModels;
using Microsoft.Git.CredentialManager;
using Application = Avalonia.Application;

namespace GitCredentialManager.UI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var appPath = ApplicationBase.GetEntryApplicationPath();
                var context = new CommandContext(appPath);
                var viewModel = new MainWindowViewModel(context);
                desktop.MainWindow = new MainWindow
                {
                    DataContext = viewModel,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
