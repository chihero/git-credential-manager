using System.CommandLine;
using System.CommandLine.Invocation;
using GitCredentialManager;

namespace Microsoft.AzureRepos.Commands
{
    public class ClearCacheCommand : Command
    {
        private readonly ICommandContext _context;
        private readonly IAzureDevOpsAuthorityCache _cache;

        public ClearCacheCommand(ICommandContext context, IAzureDevOpsAuthorityCache cache)
            : base("clear-cache", "Clear the Azure authority cache.")
        {
            _context = context;
            _cache = cache;

            Handler = CommandHandler.Create(Execute);
        }

        private int Execute()
        {
            _cache.Clear();
            _context.Streams.Out.WriteLine("Authority cache cleared");
            return 0;
        }
    }
}
