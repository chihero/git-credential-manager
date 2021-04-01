using System;
using System.Windows.Input;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace Microsoft.Authentication.UI.Login
{
    public class LoginDeviceViewModel : WindowViewModel
    {
        private Uri verificationUri;
        private string deviceCode;

        public LoginDeviceViewModel(string deviceCode, Uri verificationUri)
        {
            DeviceCode = deviceCode;
            VerificationUri = verificationUri;

            NavigateVerificationCommand = new RelayCommand(NavigateVerification);
        }

        public override bool IsValid => true;

        public override string Title => MicrosoftAuthResources.DeviceTitle;

        public Uri VerificationUri
        {
            get => this.verificationUri;
            private set => SetAndRaisePropertyChanged(ref this.verificationUri, value);
        }

        public string DeviceCode
        {
            get => this.deviceCode;
            set => SetAndRaisePropertyChanged(ref this.deviceCode, value);
        }

        public ICommand NavigateVerificationCommand { get; }

        private void NavigateVerification()
        {
            OpenDefaultBrowser(VerificationUri.ToString());
        }
    }
}
