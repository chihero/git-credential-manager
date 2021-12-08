using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Commands;
using KnownGitCfg = GitCredentialManager.Constants.GitConfiguration;

namespace Microsoft.AzureRepos
{
    public class AzureReposHostProvider : DisposableObject, IHostProvider, IConfigurableComponent, ICommandProvider
    {
        private readonly ICommandContext _context;
        private readonly IAzureDevOpsRestApi _azDevOps;
        private readonly IMicrosoftAuthentication _msAuth;
        private readonly IAzureDevOpsAuthorityCache _authorityCache;
        private readonly IAzureReposBindingManager _bindingManager;

        public AzureReposHostProvider(ICommandContext context)
            : this(context, new AzureDevOpsRestApi(context), new MicrosoftAuthentication(context),
                new AzureDevOpsAuthorityCache(context), new AzureReposBindingManager(context))
        {
        }

        public AzureReposHostProvider(ICommandContext context, IAzureDevOpsRestApi azDevOps,
            IMicrosoftAuthentication msAuth, IAzureDevOpsAuthorityCache authorityCache,
            IAzureReposBindingManager bindingManager)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(azDevOps, nameof(azDevOps));
            EnsureArgument.NotNull(msAuth, nameof(msAuth));
            EnsureArgument.NotNull(authorityCache, nameof(authorityCache));
            EnsureArgument.NotNull(bindingManager, nameof(bindingManager));

            _context = context;
            _azDevOps = azDevOps;
            _msAuth = msAuth;
            _authorityCache = authorityCache;
            _bindingManager = bindingManager;
        }

        #region IHostProvider

        public string Id => "azure-repos";

        public string Name => "Azure Repos";

        public IEnumerable<string> SupportedAuthorityIds => MicrosoftAuthentication.AuthorityIds;

        public bool IsSupported(InputArguments input)
        {
            if (input is null)
            {
                return false;
            }

            // We do not support unencrypted HTTP communications to Azure Repos,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            return input.TryGetHostAndPort(out string hostName, out _)
                   && (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                       StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https")) &&
                   UriHelpers.IsAzureDevOpsHost(hostName);
        }

        public bool IsSupported(HttpResponseMessage response)
        {
            // Azure DevOps Server (TFS) is handled by the generic provider, which supports basic auth, and WIA detection.
            return false;
        }

        public async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            Uri remoteUri = input.GetRemoteUri();

            // Include the username request here so that we may use it as an override
            // for user account lookups when getting Azure Access Tokens.
            var azureResult = await GetAzureAccessTokenAsync(remoteUri, input.UserName);
            return new GitCredential(azureResult.AccountUpn, azureResult.AccessToken);
        }

        public Task StoreCredentialAsync(InputArguments input)
        {
            Uri remoteUri = input.GetRemoteUri();

            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            _context.Trace.WriteLine($"Signing user {input.UserName} in to organization '{orgName}'...");
            _bindingManager.SignIn(orgName, input.UserName);

            return Task.CompletedTask;
        }

        public Task EraseCredentialAsync(InputArguments input)
        {
            Uri remoteUri = input.GetRemoteUri();

            string orgName = UriHelpers.GetOrganizationName(remoteUri);

            _context.Trace.WriteLine($"Signing out of organization '{orgName}'...");
            _bindingManager.SignOut(orgName);

            // Clear the authority cache in case this was the reason for failure
            _authorityCache.EraseAuthority(orgName);

            return Task.CompletedTask;
        }

        protected override void ReleaseManagedResources()
        {
            _azDevOps.Dispose();
            base.ReleaseManagedResources();
        }

        private async Task<IMicrosoftAuthenticationResult> GetAzureAccessTokenAsync(Uri remoteUri, string userName)
        {
            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(remoteUri.Scheme, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for Azure Repos. Ensure the repository remote URL is using HTTPS.");
            }

            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string orgName);

            _context.Trace.WriteLine($"Determining Microsoft Authentication authority for Azure DevOps organization '{orgName}'...");
            string authAuthority = _authorityCache.GetAuthority(orgName);
            if (authAuthority is null)
            {
                // If there is no cached value we must query for it and cache it for future use
                _context.Trace.WriteLine($"No cached authority value - querying {orgUri} for authority...");
                authAuthority = await _azDevOps.GetAuthorityAsync(orgUri);
                _authorityCache.UpdateAuthority(orgName, authAuthority);
            }
            _context.Trace.WriteLine($"Authority is '{authAuthority}'.");

            //
            // If the remote URI is a classic "*.visualstudio.com" host name and we have a user specified from the
            // remote then take that as the current AAD/MSA user in the first instance.
            //
            // For "dev.azure.com" host names we only use the user info part of the remote when this doesn't
            // match the Azure DevOps organization name. Our friends in Azure DevOps decided "borrow" the username
            // part of the remote URL to include the organization name (not an actual username).
            //
            // If we have no specified user from the remote (or this is org@dev.azure.com/org/..) then query the
            // user manager for a bound user for this organization, if one exists...
            //
            var icmp = StringComparer.OrdinalIgnoreCase;
            if (!string.IsNullOrWhiteSpace(userName) &&
                (UriHelpers.IsVisualStudioComHost(remoteUri.Host) ||
                 (UriHelpers.IsAzureDevOpsHost(remoteUri.Host) && !icmp.Equals(orgName, userName))))
            {
                _context.Trace.WriteLine("Using username as specified in remote.");
            }
            else
            {
                _context.Trace.WriteLine($"Looking up user for organization '{orgName}'...");
                userName = _bindingManager.GetUser(orgName);
            }

            _context.Trace.WriteLine(string.IsNullOrWhiteSpace(userName) ? "No user found." : $"User is '{userName}'.");

            // Get an AAD access token for the Azure DevOps SPS
            _context.Trace.WriteLine("Getting Azure AD access token...");
            IMicrosoftAuthenticationResult result = await _msAuth.GetTokenAsync(
                authAuthority,
                GetClientId(),
                GetRedirectUri(),
                AzureDevOpsConstants.AzureDevOpsDefaultScopes,
                userName);
            _context.Trace.WriteLineSecrets(
                $"Acquired Azure access token. Account='{result.AccountUpn}' Token='{{0}}'", new object[] {result.AccessToken});

            return result;
        }

        private string GetClientId()
        {
            // Check for developer override value
            if (_context.Settings.TryGetSetting(
                    AzureDevOpsConstants.EnvironmentVariables.DevAadClientId,
                    Constants.GitConfiguration.Credential.SectionName,
                    AzureDevOpsConstants.GitConfiguration.Credential.DevAadClientId,
                    out string clientId))
            {
                return clientId;
            }

            return AzureDevOpsConstants.AadClientId;
        }

        private Uri GetRedirectUri()
        {
            // Check for developer override value
            if (_context.Settings.TryGetSetting(
                    AzureDevOpsConstants.EnvironmentVariables.DevAadRedirectUri,
                    Constants.GitConfiguration.Credential.SectionName, AzureDevOpsConstants.GitConfiguration.Credential.DevAadRedirectUri,
                    out string redirectUriStr) &&
                Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return AzureDevOpsConstants.AadRedirectUri;
        }

        #endregion

        #region IConfigurationComponent

        string IConfigurableComponent.Name => "Azure Repos provider";

        public Task ConfigureAsync(ConfigurationTarget target)
        {
            string useHttpPathKey = $"{KnownGitCfg.Credential.SectionName}.https://dev.azure.com.{KnownGitCfg.Credential.UseHttpPath}";

            GitConfigurationLevel configurationLevel = target == ConfigurationTarget.System
                ? GitConfigurationLevel.System
                : GitConfigurationLevel.Global;

            IGitConfiguration targetConfig = _context.Git.GetConfiguration();

            if (targetConfig.TryGet(useHttpPathKey, false, out string currentValue) && currentValue.IsTruthy())
            {
                _context.Trace.WriteLine("Git configuration 'credential.useHttpPath' is already set to 'true' for https://dev.azure.com.");
            }
            else
            {
                _context.Trace.WriteLine("Setting Git configuration 'credential.useHttpPath' to 'true' for https://dev.azure.com...");
                targetConfig.Set(configurationLevel, useHttpPathKey, "true");
            }

            return Task.CompletedTask;
        }

        public Task UnconfigureAsync(ConfigurationTarget target)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
            string useHttpPathKey = $"{KnownGitCfg.Credential.SectionName}.https://dev.azure.com.{KnownGitCfg.Credential.UseHttpPath}";

            _context.Trace.WriteLine("Clearing Git configuration 'credential.useHttpPath' for https://dev.azure.com...");

            GitConfigurationLevel configurationLevel = target == ConfigurationTarget.System
                ? GitConfigurationLevel.System
                : GitConfigurationLevel.Global;

            IGitConfiguration targetConfig = _context.Git.GetConfiguration();

            // On Windows, if there is a "manager-core" entry remaining in the system config then we must not clear
            // the useHttpPath option otherwise this would break the bundled version of GCM in Git for Windows.
            if (!PlatformUtils.IsWindows() || target != ConfigurationTarget.System ||
                targetConfig.GetAll(helperKey).All(x => !string.Equals(x, "manager-core")))
            {
                targetConfig.Unset(configurationLevel, useHttpPathKey);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region ICommandProvider

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
            listCmd.AddOption(new Option(new[] {"--verbose", "-v"})
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
                        var binding = new RemoteBinding {IsPush = false, Remote = remote.Name, Uri = fetchUri};
                        orgRemotesMap.Append(fetchOrg, binding);
                    }

                    if (IsAzureDevOpsHttpRemote(remote.PushUrl, out Uri pushUri))
                    {
                        string pushOrg = UriHelpers.GetOrganizationName(pushUri);
                        var binding = new RemoteBinding {IsPush = true, Remote = remote.Name, Uri = pushUri};
                        orgRemotesMap.Append(pushOrg, binding);
                    }
                }
            }

            bool isFiltered = organization != null;
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

        #endregion
    }
}
