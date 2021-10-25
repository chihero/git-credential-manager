using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        private readonly ICommandContext _context;

        public MainWindowViewModel()
        {
            // Constructor the XAML designer
        }

        public MainWindowViewModel(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public string Greeting => "Welcome to Avalonia!";

        public string GitVersion => _context.Git.Version.ToString();
    }
}
