// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
using Microsoft.Git.CredentialManager.Interop;

namespace Microsoft.Git.CredentialManager
{
    public class Application : ApplicationBase, IConfigurableComponent
    {
        private readonly string _appPath;
        private readonly IHostProviderRegistry _providerRegistry;
        private readonly IConfigurationService _configurationService;
        private readonly IList<ProviderCommand> _providerCommands = new List<ProviderCommand>();

        public Application(ICommandContext context, string appPath)
            : this(context, new HostProviderRegistry(context), new ConfigurationService(context), appPath)
        {
        }

        internal Application(ICommandContext context,
                             IHostProviderRegistry providerRegistry,
                             IConfigurationService configurationService,
                             string appPath)
            : base(context)
        {
            EnsureArgument.NotNull(providerRegistry, nameof(providerRegistry));
            EnsureArgument.NotNull(configurationService, nameof(configurationService));
            EnsureArgument.NotNullOrWhiteSpace(appPath, nameof(appPath));

            _appPath = appPath;
            _providerRegistry = providerRegistry;
            _configurationService = configurationService;

            _configurationService.AddComponent(this);
        }

        public void RegisterProvider(IHostProvider provider, HostProviderPriority priority)
        {
            _providerRegistry.Register(provider, priority);

            // If the provider is also a configurable component, add that to the configuration service
            if (provider is IConfigurableComponent configurableProvider)
            {
                _configurationService.AddComponent(configurableProvider);
            }

            // If the provider has custom commands to offer then create a root command for the provider
            if (provider is ICommandProvider cmdProvider)
            {
                var providerCommand = new ProviderCommand(Context, provider);
                cmdProvider.ConfigureCommand(providerCommand);
                _providerCommands.Add(providerCommand);
            }

        }

        protected override async Task<int> RunInternalAsync(string[] args)
        {
            var rootCommand = new RootCommand();

            var getCommand = new GetCommand(Context, _providerRegistry);
            var storeCommand = new StoreCommand(Context, _providerRegistry);
            var eraseCommand = new EraseCommand(Context, _providerRegistry);
            var configureCommand = new ConfigureCommand(Context, _configurationService);
            var unconfigureCommand = new UnconfigureCommand(Context, _configurationService);

            // Add standard commands
            rootCommand.AddCommand(getCommand);
            rootCommand.AddCommand(storeCommand);
            rootCommand.AddCommand(eraseCommand);
            rootCommand.AddCommand(configureCommand);
            rootCommand.AddCommand(unconfigureCommand);

            // Add any custom provider commands
            foreach (ProviderCommand providerCommand in _providerCommands)
            {
                rootCommand.AddCommand(providerCommand);
            }

            // Trace the current version and program arguments
            Context.Trace.WriteLine($"{Constants.GetProgramHeader()} '{string.Join(" ", args)}'");

            try
            {
                return await rootCommand.InvokeAsync(args);
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    ae.Handle(WriteException);
                }
                else
                {
                    WriteException(e);
                }

                return -1;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _providerRegistry?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected bool WriteException(Exception ex)
        {
            // Try and use a nicer format for some well-known exception types
            switch (ex)
            {
                case InteropException interopEx:
                    Context.Streams.Error.WriteLine("fatal: {0} [0x{1:x}]", interopEx.Message, interopEx.ErrorCode);
                    break;
                default:
                    Context.Streams.Error.WriteLine("fatal: {0}", ex.Message);
                    break;
            }

            // Recurse to print all inner exceptions
            if (!(ex.InnerException is null))
            {
                WriteException(ex.InnerException);
            }

            return true;
        }

        #region IConfigurableComponent

        string IConfigurableComponent.Name => "Git Credential Manager";

        Task IConfigurableComponent.ConfigureAsync(ConfigurationTarget target)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
            string appPath = GetGitConfigAppPath();

            GitConfigurationLevel configLevel = target == ConfigurationTarget.System
                    ? GitConfigurationLevel.System
                    : GitConfigurationLevel.Global;

            Context.Trace.WriteLine($"Configuring for config level '{configLevel}'.");

            IGitConfiguration config = Context.Git.GetConfiguration(configLevel);

            // We are looking for the following to be set in the config:
            //
            // [credential]
            //     ...                # any number of helper entries (possibly none)
            //     helper =           # an empty value to reset/clear any previous entries (if applicable)
            //     helper = {appPath} # the expected executable value & directly following the empty value
            //     ...                # any number of helper entries (possibly none, but not the empty value '')
            //
            string[] currentValues = config.GetAll(helperKey).ToArray();

            // Try to locate an existing app entry with a blank reset/clear entry immediately preceding,
            // and no other blank empty/clear entries following (which effectively disable us).
            int appIndex = Array.FindIndex(currentValues, x => Context.FileSystem.IsSamePath(x, appPath));
            int lastEmptyIndex = Array.FindLastIndex(currentValues, string.IsNullOrWhiteSpace);
            if (appIndex > 0 && string.IsNullOrWhiteSpace(currentValues[appIndex - 1]) && lastEmptyIndex < appIndex)
            {
                Context.Trace.WriteLine("Credential helper configuration is already set correctly.");
            }
            else
            {
                Context.Trace.WriteLine("Updating Git credential helper configuration...");

                // Clear any existing app entries in the configuration
                config.UnsetAll(helperKey, Regex.Escape(appPath));

                // Add an empty value for `credential.helper`, which has the effect of clearing any helper value
                // from any lower-level Git configuration, then add a second value which is the actual executable path.
                config.Add(helperKey, string.Empty);
                config.Add(helperKey, appPath);
            }

            return Task.CompletedTask;
        }

        Task IConfigurableComponent.UnconfigureAsync(ConfigurationTarget target)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
            string appPath = GetGitConfigAppPath();

            GitConfigurationLevel configLevel = target == ConfigurationTarget.System
                    ? GitConfigurationLevel.System
                    : GitConfigurationLevel.Global;

            Context.Trace.WriteLine($"Unconfiguring for config level '{configLevel}'.");

            IGitConfiguration config = Context.Git.GetConfiguration(configLevel);

            // We are looking for the following to be set in the config:
            //
            // [credential]
            //     ...                 # any number of helper entries (possibly none)
            //     helper =            # an empty value to reset/clear any previous entries (if applicable)
            //     helper = {appPath} # the expected executable value & directly following the empty value
            //     ...                 # any number of helper entries (possibly none)
            //
            // We should remove the {appPath} entry, and any blank entries immediately preceding IFF there are no more entries following.
            //
            Context.Trace.WriteLine("Removing Git credential helper configuration...");

            string[] currentValues = config.GetAll(helperKey).ToArray();

            int appIndex = Array.FindIndex(currentValues, x => Context.FileSystem.IsSamePath(x, appPath));
            if (appIndex > -1)
            {
                // Check for the presence of a blank entry immediately preceding an app entry in the last position
                if (appIndex > 0 && appIndex == currentValues.Length - 1 &&
                    string.IsNullOrWhiteSpace(currentValues[appIndex - 1]))
                {
                    // Clear the blank entry
                    config.UnsetAll(helperKey, Constants.RegexPatterns.Empty);
                }

                // Clear app entry
                config.UnsetAll(helperKey, Regex.Escape(appPath));
            }

            return Task.CompletedTask;
        }

        private string GetGitConfigAppName()
        {
            const string gitCredentialPrefix = "git-credential-";

            string appName = Path.GetFileNameWithoutExtension(_appPath);
            if (appName != null && appName.StartsWith(gitCredentialPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return appName.Substring(gitCredentialPrefix.Length);
            }

            return _appPath;
        }

        private string GetGitConfigAppPath()
        {
            string path = _appPath;

            // On Windows we must use UNIX style path separators
            if (PlatformUtils.IsWindows())
            {
                path = path.Replace('\\', '/');
            }

            // We must escape escape characters like ' ', '(', and ')'
            return path
                .Replace(" ", "\\ ")
                .Replace("(", "\\(")
                .Replace(")", "\\)");;
        }

        #endregion
    }
}
