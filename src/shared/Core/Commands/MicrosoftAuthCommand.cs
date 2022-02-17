using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using GitCredentialManager.Authentication;

namespace GitCredentialManager.Commands
{
    public class MicrosoftAuthCommand : Command
    {
        private readonly ICommandContext _context;

        public MicrosoftAuthCommand(ICommandContext context)
            : base("msid", "Manage Microsoft identity platform accounts and settings.")
        {
            EnsureArgument.NotNull(context, nameof(context));
            _context = context;

            var accountListCmd = new Command("accounts", "List cached accounts.")
            {
                Handler = CommandHandler.Create(AccountListAsync)
            };

            var brokerCmd = new Command("broker", "Manage authentication broker settings.");
            var brokerStatus = new Command("status", "Show authentication broker status.")
            {
                Handler = CommandHandler.Create(() =>
                {
                    _context.Git.GetConfiguration().TryGet("credential.msauthUseBroker", false, out string value);
                    _context.Streams.Out.WriteLine($"Enabled: {value}");
                })
            };
            var brokerEnable = new Command("enable", "Enable authentication broker.")
            {
                Handler = CommandHandler.Create(() => _context.Git.GetConfiguration().Set(GitConfigurationLevel.Global, "credential.msauthUseBroker", "true"))
            };
            var brokerDisable = new Command("disable", "Disable authentication broker.")
            {
                Handler = CommandHandler.Create(() => _context.Git.GetConfiguration().Set(GitConfigurationLevel.Global, "credential.msauthUseBroker", "false"))
            };
            brokerCmd.AddCommand(brokerEnable);
            brokerCmd.AddCommand(brokerDisable);
            brokerCmd.AddCommand(brokerStatus);

            AddCommand(accountListCmd);
            AddCommand(brokerCmd);
        }

        private async Task<int> AccountListAsync()
        {
            var msAuth = new MicrosoftAuthentication(_context);

            IEnumerable<IMicrosoftAccount> accounts = await msAuth.GetAccountsAsync(Constants.GcmAadClientId);

            foreach (IMicrosoftAccount account in accounts)
            {
                await _context.Streams.Out.WriteLineAsync($"Identifier     : {account.Id}");
                await _context.Streams.Out.WriteLineAsync($"UPN            : {account.UserName}");
                await _context.Streams.Out.WriteLineAsync($"Home Tenant ID : {account.HomeTenantId}");
                await _context.Streams.Out.WriteLineAsync($"Environment    : {account.Environment}");
                await _context.Streams.Out.WriteLineAsync();
            }

            return 0;
        }
    }
}
