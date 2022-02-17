using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using GitCredentialManager;
using GitCredentialManager.Commands;

namespace Microsoft.AzureRepos
{
    public partial class AzureReposHostProvider : ICommandProvider
    {
        ProviderCommand ICommandProvider.CreateCommand()
        {
            var clearCacheCmd = new Command("clear-cache")
            {
                Description = "Clear the Azure authority cache",
                Handler = CommandHandler.Create(ClearCacheCmd),
            };

            var orgArg = new Argument("organization")
            {
                Arity = ArgumentArity.ExactlyOne,
                Description = "Azure DevOps organization name"
            };
            var localOpt = new Option("--local")
            {
                Description = "Target the local repository Git configuration"
            };

            var listCmd = new Command("list", "List all user account bindings")
            {
                Handler = CommandHandler.Create<string, bool, bool>(ListCmd)
            };
            listCmd.AddArgument(new Argument("organization")
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "(optional) Filter results by Azure DevOps organization name"
            });
            listCmd.AddOption(new Option("--show-remotes")
            {
                Description = "Also show Azure DevOps remote user bindings for the current repository"
            });
            listCmd.AddOption(new Option(new[] { "--verbose", "-v" })
            {
                Description = "Verbose output - show remote URLs"
            });

            var bindCmd = new Command("bind")
            {
                Description = "Bind a user account to an Azure DevOps organization",
                Handler = CommandHandler.Create<string, string, bool>(BindCmd),
            };
            bindCmd.AddArgument(orgArg);
            bindCmd.AddArgument(new Argument("username")
            {
                Arity = ArgumentArity.ExactlyOne,
                Description = "Username or email (e.g.: alice@example.com)"
            });
            bindCmd.AddOption(localOpt);

            var unbindCmd = new Command("unbind")
            {
                Description = "Remove user account binding for an Azure DevOps organization",
                Handler = CommandHandler.Create<string, bool>(UnbindCmd),
            };
            unbindCmd.AddArgument(orgArg);
            unbindCmd.AddOption(localOpt);

            var rootCmd = new ProviderCommand(this);
            rootCmd.AddCommand(listCmd);
            rootCmd.AddCommand(bindCmd);
            rootCmd.AddCommand(unbindCmd);
            rootCmd.AddCommand(clearCacheCmd);
            return rootCmd;
        }

        private int ClearCacheCmd()
        {
            _authorityCache.Clear();
            _context.Streams.Out.WriteLine("Authority cache cleared");
            return 0;
        }

        private class RemoteBinding
        {
            public string Remote { get; set; }
            public bool IsPush { get; set; }
            public Uri Uri { get; set; }
        }

        private int ListCmd(string organization, bool showRemotes, bool verbose)
        {
            // Get all organization bindings from the user manager
            IList<AzureReposBinding> bindings = _bindingManager.GetBindings(organization).ToList();
            IDictionary<string, IEnumerable<AzureReposBinding>> orgBindingMap =
                bindings.GroupBy(x => x.Organization).ToDictionary();

            // If we are asked to also show remotes we build the remote binding map
            var orgRemotesMap = new Dictionary<string, ICollection<RemoteBinding>>();
            if (showRemotes)
            {
                if (!_context.Git.IsInsideRepository())
                {
                    _context.Streams.Error.WriteLine("warning: not inside a git repository (--show-remotes has no effect)");
                }

                static bool IsAzureDevOpsHttpRemote(string url, out Uri uri)
                {
                    return Uri.TryCreate(url, UriKind.Absolute, out uri) &&
                           (StringComparer.OrdinalIgnoreCase.Equals(Uri.UriSchemeHttp, uri.Scheme) ||
                            StringComparer.OrdinalIgnoreCase.Equals(Uri.UriSchemeHttps, uri.Scheme)) &&
                           UriHelpers.IsAzureDevOpsHost(uri.Host);
                }

                foreach (GitRemote remote in _context.Git.GetRemotes())
                {
                    if (IsAzureDevOpsHttpRemote(remote.FetchUrl, out Uri fetchUri))
                    {
                        string fetchOrg = UriHelpers.GetOrganizationName(fetchUri);
                        var binding = new RemoteBinding { IsPush = false, Remote = remote.Name, Uri = fetchUri };
                        orgRemotesMap.Append(fetchOrg, binding);
                    }

                    if (IsAzureDevOpsHttpRemote(remote.PushUrl, out Uri pushUri))
                    {
                        string pushOrg = UriHelpers.GetOrganizationName(pushUri);
                        var binding = new RemoteBinding { IsPush = true, Remote = remote.Name, Uri = pushUri };
                        orgRemotesMap.Append(pushOrg, binding);
                    }
                }
            }

            bool isFiltered = !string.IsNullOrWhiteSpace(organization);
            string indent = isFiltered ? string.Empty : "  ";

            // Get the set of all organization names (organization names are not case sensitive)
            ISet<string> orgNames = new HashSet<string>(orgBindingMap.Keys, StringComparer.OrdinalIgnoreCase);
            orgNames.UnionWith(orgRemotesMap.Keys);

            var icmp = StringComparer.OrdinalIgnoreCase;

            foreach (string orgName in orgNames)
            {
                if (!isFiltered)
                {
                    _context.Streams.Out.WriteLine($"{orgName}:");
                }

                // Print organization bindings
                foreach (AzureReposBinding binding in orgBindingMap.GetValues(orgName))
                {
                    if (binding.GlobalUserName != null)
                    {
                        _context.Streams.Out.WriteLine($"{indent}(global) -> {binding.GlobalUserName}");
                    }

                    if (binding.LocalUserName != null)
                    {
                        string value = string.IsNullOrEmpty(binding.LocalUserName)
                            ? "(no inherit)"
                            : binding.LocalUserName;
                        _context.Streams.Out.WriteLine($"{indent}(local)  -> {value}");
                    }
                }

                // Print remote bindings
                IEnumerable<IGrouping<string, RemoteBinding>> remoteBindingMap =
                    orgRemotesMap.GetValues(orgName).GroupBy(x => x.Remote);

                foreach (var remoteBinding in remoteBindingMap)
                {
                    _context.Streams.Out.WriteLine($"{indent}{remoteBinding.Key}:");
                    foreach (RemoteBinding binding in remoteBinding)
                    {
                        // User names in dev.azure.com URLs cannot always be used as *actual user names*
                        // because of the unfortunate decision to use this field to get the Azure DevOps
                        // organization name to be sent by Git to credential helpers.
                        //
                        // We show dev.azure.com URLs as "inherit", if there is a username that matches
                        // the organization name.
                        if (!binding.Uri.TryGetUserInfo(out string userName, out _) ||
                            UriHelpers.IsDevAzureComHost(binding.Uri.Host) && icmp.Equals(userName, orgName))
                        {
                            userName = "(inherit)";
                        }

                        string url = null;
                        if (verbose)
                        {
                            url = $"{binding.Uri.WithoutUserInfo()} ";
                        }

                        _context.Streams.Out.WriteLine(binding.IsPush
                            ? $"{indent}  {url}(push)  -> {userName}"
                            : $"{indent}  {url}(fetch) -> {userName}");
                    }
                }
            }

            return 0;
        }

        private int BindCmd(string organization, string userName, bool local)
        {
            if (local && !_context.Git.IsInsideRepository())
            {
                _context.Streams.Error.WriteLine("error: not inside a git repository (cannot use --local)");
                return -1;
            }

            _bindingManager.Bind(organization, userName, local);
            return 0;
        }

        private int UnbindCmd(string organization, bool local)
        {
            if (local && !_context.Git.IsInsideRepository())
            {
                _context.Streams.Error.WriteLine("error: not inside a git repository (cannot use --local)");
                return -1;
            }

            _bindingManager.Unbind(organization, local);
            return 0;
        }
    }
}
