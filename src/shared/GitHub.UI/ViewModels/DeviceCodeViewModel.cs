using System.Windows.Input;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace GitHub.UI.ViewModels
{
    public class DeviceCodeViewModel : WindowViewModel
    {
        private readonly IEnvironment _environment;

        private string _code;
        private string _verificationUrl;
        private ICommand _verificationLinkCommand;
        private ICommand _cancelCommand;

        public DeviceCodeViewModel()
        {
            // For designer
        }

        public DeviceCodeViewModel(IEnvironment environment, string code, string verificationUrl)
        {
            EnsureArgument.NotNull(environment, nameof(environment));
            EnsureArgument.NotNullOrWhiteSpace(code, nameof(code));
            EnsureArgument.NotNullOrWhiteSpace(verificationUrl, nameof(verificationUrl));

            _environment = environment;

            Code = code;
            VerificationUrl = verificationUrl;

            Title = "Device authentication";
            VerificationLinkCommand = new RelayCommand(OpenVerificationLink);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void OpenVerificationLink()
        {
            BrowserUtils.OpenDefaultBrowser(_environment, VerificationUrl);
        }

        public string Code
        {
            get => _code;
            set => SetAndRaisePropertyChanged(ref _code, value);
        }

        public string VerificationUrl
        {
            get => _verificationUrl;
            set => SetAndRaisePropertyChanged(ref _verificationUrl, value);
        }

        public ICommand VerificationLinkCommand
        {
            get => _verificationLinkCommand;
            set => SetAndRaisePropertyChanged(ref _verificationLinkCommand, value);
        }

        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set => SetAndRaisePropertyChanged(ref _cancelCommand, value);
        }
    }
}
