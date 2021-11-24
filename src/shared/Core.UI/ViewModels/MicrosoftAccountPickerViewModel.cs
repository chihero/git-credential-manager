using System.Collections.Generic;

namespace GitCredentialManager.UI.ViewModels
{
    public class MicrosoftAccountPickerViewModel : WindowViewModel
    {
        private IList<string> _accounts = new List<string>();
        private string _selectedAccount;
        private RelayCommand _selectCommand;
        private RelayCommand _addAccountCommand;

        public MicrosoftAccountPickerViewModel()
        {
            SelectCommand = new RelayCommand(Accept, CanSelect);
            AddAccountCommand = new RelayCommand(AddAccount);
        }

        private bool CanSelect()
        {
            return SelectedAccount != null;
        }

        private void AddAccount()
        {
            // A null selected account means pick a new account
            SelectedAccount = null;
            Accept();
        }

        public IList<string> Accounts
        {
            get => _accounts;
            set => SetAndRaisePropertyChanged(ref _accounts, value);
        }

        public string SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                SetAndRaisePropertyChanged(ref _selectedAccount, value);
                SelectCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand SelectCommand
        {
            get => _selectCommand;
            set => SetAndRaisePropertyChanged(ref _selectCommand, value);
        }

        public RelayCommand AddAccountCommand
        {
            get => _addAccountCommand;
            set => SetAndRaisePropertyChanged(ref _addAccountCommand, value);
        }
    }
}
