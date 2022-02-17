using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.UI;
using Microsoft.AzureRepos.UI.ViewModels;

namespace Microsoft.AzureRepos.UI.Commands
{
    public abstract class AccountPickerCommand : HelperCommand
    {
        protected AccountPickerCommand(ICommandContext context)
            : base(context, "select-account", "Show account picker.")
        {
            Handler = CommandHandler.Create<CommandOptions>(ExecuteAsync);
        }

        private class CommandOptions
        {
            // No args
        }

        private async Task<int> ExecuteAsync(CommandOptions options)
        {
            var viewModel = new AccountPickerViewModel();

            // Deserialize JSON accounts from standard input
            IDictionary<string, IList<string>> input = await Context.Streams.In.ReadMultiDictionaryAsync(StringComparer.OrdinalIgnoreCase);

            if (!input.TryGetValue("account", out IList<string> accountLines) || accountLines.Count == 0)
            {
                throw new Exception("Missing account information on standard input");
            }

            var accounts = new List<AccountViewModel>();

            foreach (string line in accountLines)
            {
                var account = new AccountViewModel();

                string[] parts = line?.Split(';');

                if (parts is null || parts.Length == 0)
                {
                    continue;
                }

                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    account.UserName = parts[0];
                }
                else
                {
                    await Context.Streams.Error.WriteLineAsync("warning: malformed account information - missing username");
                    continue;
                }

                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    account.DisplayName = parts[1];
                }

                if (parts.Length > 2 && parts[2].IsTruthy())
                {
                    account.IsPersonalAccount = true;
                }

                accounts.Add(account);
            }

            if (accounts.Count == 0)
            {
                throw new Exception("Must provide at least one account");
            }

            viewModel.Accounts = new ObservableCollection<AccountViewModel>(accounts);

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }

            var result = new Dictionary<string, string>();

            if (viewModel.UseAccountForAllOrganizations)
            {
                result["allorgs"] = "true";
            }

            if (viewModel.AddNewAccount)
            {
                result["new"] = "true";
            }
            else
            {
                result["account"] = viewModel.SelectedAccount?.UserName;
            }

            WriteResult(result);
            return 0;
        }

        protected abstract Task ShowAsync(AccountPickerViewModel viewModel, CancellationToken ct);
    }
}
