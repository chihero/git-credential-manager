using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.Commands
{
    public abstract class MicrosoftAccountPickerCommand : HelperCommand
    {
        protected MicrosoftAccountPickerCommand(ICommandContext context)
            : base(context, "pick", "Select an account")
        {
            AddArgument(new Argument<string>("accounts")
            {
                Arity = ArgumentArity.ZeroOrMore
            });

            Handler = CommandHandler.Create<CommandOptions>(ExecuteAsync);
        }

        private class CommandOptions
        {
            public IList<string> Accounts { get; set; }
        }

        private async Task<int> ExecuteAsync(CommandOptions options)
        {
            var viewModel = new MicrosoftAccountPickerViewModel
            {
                Accounts = options.Accounts
            };

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }

            WriteResult(new Dictionary<string, string>
            {
                ["account"] = viewModel.SelectedAccount
            });

            return 0;
        }

        protected abstract Task ShowAsync(MicrosoftAccountPickerViewModel viewModel, CancellationToken ct);
    }
}
