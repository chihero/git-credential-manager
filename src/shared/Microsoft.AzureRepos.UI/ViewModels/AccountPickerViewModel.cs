using System.Collections.ObjectModel;
using System.ComponentModel;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;

namespace Microsoft.AzureRepos.UI.ViewModels
{
    public class AccountPickerViewModel : WindowViewModel
    {
        private bool _useAllOrgs;
        private RelayCommand _addCommand;
        private RelayCommand _continueCommand;
        private ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
        private AccountViewModel _selectedAccount;

        public AccountPickerViewModel()
        {
            Title = "Select an account";
            AddCommand = new RelayCommand(AddAccount);
            ContinueCommand = new RelayCommand(SelectAccount, CanSelect);

            PropertyChanged += OnPropertyChanged;
        }

        private void AddAccount()
        {
            AddNewAccount = true;
            SelectedAccount = null;
            Accept();
        }

        private void SelectAccount()
        {
            AddNewAccount = false;
            Accept();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedAccount):
                    ContinueCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        private bool CanSelect()
        {
            return SelectedAccount != null;
        }

        public bool UseAccountForAllOrganizations
        {
            get => _useAllOrgs;
            set => SetAndRaisePropertyChanged(ref _useAllOrgs, value);
        }

        public ObservableCollection<AccountViewModel> Accounts
        {
            get => _accounts;
            set => SetAndRaisePropertyChanged(ref _accounts, value);
        }

        public AccountViewModel SelectedAccount
        {
            get => _selectedAccount;
            set => SetAndRaisePropertyChanged(ref _selectedAccount, value);
        }

        public RelayCommand AddCommand
        {
            get => _addCommand;
            set => SetAndRaisePropertyChanged(ref _addCommand, value);
        }

        public RelayCommand ContinueCommand
        {
            get => _continueCommand;
            set => SetAndRaisePropertyChanged(ref _continueCommand, value);
        }

        public bool AddNewAccount { get; set; }
    }

    public class AccountViewModel : ViewModel
    {
        private bool _isPersonalAccount;
        private string _userName;
        private string _displayName;

        public bool IsPersonalAccount
        {
            get => _isPersonalAccount;
            set => SetAndRaisePropertyChanged(ref _isPersonalAccount, value);
        }

        public string UserName
        {
            get => _userName;
            set => SetAndRaisePropertyChanged(ref _userName, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetAndRaisePropertyChanged(ref _displayName, value);
        }
    }
}
