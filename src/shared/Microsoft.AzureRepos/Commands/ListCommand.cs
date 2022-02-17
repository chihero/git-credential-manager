using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using GitCredentialManager;

namespace Microsoft.AzureRepos.Commands
{
    public class ListCommand : Command
    {
        private readonly ICommandContext _context;
        private readonly IAzureReposUserManager _userManager;

        public ListCommand(ICommandContext context, IAzureReposUserManager userManager)
            : base("list", "List all signed-in user accounts.")
        {
            _context = context;
            _userManager = userManager;

            Handler = CommandHandler.Create(Execute);
        }

        private int Execute()
        {
            IEnumerable<IGrouping<string, AzureReposUserBinding>> bindingsByUser =_userManager.GetUserBindings()
                .GroupBy(x => x.UserName);

            TextWriter stdout = _context.Streams.Out;

            foreach (IGrouping<string,AzureReposUserBinding> userBindings in bindingsByUser)
            {
                stdout.WriteLine("Username: {0}", userBindings.Key);

                var orgBindings = userBindings.OfType<AzureReposOrgBinding>().ToList();
                if (orgBindings.Any())
                {
                    stdout.WriteLine("Organizations:");
                    foreach (AzureReposOrgBinding orgBinding in orgBindings)
                    {
                        stdout.WriteLine(" {0}", orgBinding.Organization);
                    }
                }

                var authorityBindings = userBindings.OfType<AzureReposAuthorityBinding>().ToList();
                if (authorityBindings.Any())
                {
                    stdout.WriteLine("Default account for:");
                    foreach (AzureReposAuthorityBinding authorityBinding in authorityBindings)
                    {
                        stdout.WriteLine(" {0}", authorityBinding.Authority);
                    }
                }

                stdout.WriteLine();
            }

            return 0;
        }
    }
}
