using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using GitHub.UI.ViewModels;
using GitHub.UI.Views;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace GitHub.UI.Commands
{
    public class DeviceCodeCommand : HelperCommand
    {
        public DeviceCodeCommand(ICommandContext context)
            : base(context, "device", "Show device code prompt.")
        {
            AddArgument(
                new Argument<string>("code", "User code for device authentication.")
                {
                    Arity = ArgumentArity.ExactlyOne
                }
            );

            AddArgument(
                new Argument<string>("verification-url", "Device authentication verification URL.")
                {
                    Arity = ArgumentArity.ExactlyOne
                }
            );

            Handler = CommandHandler.Create<string, string>(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(string code, string verificationUrl)
        {
            var viewModel = new DeviceCodeViewModel(Context.Environment, code, verificationUrl);
            await AvaloniaUi.ShowViewAsync<DeviceCodeView>(viewModel, GetParentHandle(), CancellationToken.None);

            WriteResult(new Dictionary<string,string>
            {
                ["cancel"] = (!viewModel.WindowResult).ToString()
            });

            return 0;
        }
    }
}
