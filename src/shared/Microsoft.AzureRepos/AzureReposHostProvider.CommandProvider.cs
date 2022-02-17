using GitCredentialManager.Commands;
using Microsoft.AzureRepos.Commands;

namespace Microsoft.AzureRepos
{
    public partial class AzureReposHostProvider : ICommandProvider
    {
        ProviderCommand ICommandProvider.CreateCommand()
        {
            var userManager = new AzureReposUserManager();

            var rootCmd = new ProviderCommand(this);
            rootCmd.AddCommand(new ClearCacheCommand(_context, _authorityCache));
            rootCmd.AddCommand(new ListCommand(_context, userManager));
            return rootCmd;
        }
    }
}
