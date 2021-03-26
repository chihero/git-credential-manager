// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Reflection;
using Atlassian.Bitbucket;
using GitHub;
using Microsoft.Authentication;
using Microsoft.AzureRepos;

namespace Microsoft.Git.CredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string appPath = GetApplicationPath();
            using (var context = new CommandContext(appPath))
            using (var app = new Application(context))
            {
                IBasicPrompts basicUi = new BasicTerminalPrompts(context);
                IBitbucketPrompts bbUi = new BitbucketTerminalPrompts(context);
                IGitHubPrompts ghUi = new GitHubTerminalPrompts(context);
                IMicrosoftPrompts msUi = new MicrosoftTerminalPrompts(context);

#if WINDOWS
                if (context.SessionManager.IsDesktopSession)
                {
                    var gui = new UI.Gui();
                    basicUi = new Interop.Windows.WindowsBasicPrompts(context.Settings);
                    bbUi = new Atlassian.Bitbucket.UI.BitbucketWpfPrompts(context.Settings, gui);
                    ghUi = new GitHub.UI.GitHubWpfPrompts(context.Settings, gui);
                }
#endif

                // Register all supported host providers at the normal priority.
                // The generic provider should never win against a more specific one, so register it with low priority.
                app.RegisterProvider(new AzureReposHostProvider(context, msUi), HostProviderPriority.Normal);
                app.RegisterProvider(new BitbucketHostProvider(context, bbUi),  HostProviderPriority.Normal);
                app.RegisterProvider(new GitHubHostProvider(context, ghUi),     HostProviderPriority.Normal);
                app.RegisterProvider(new GenericHostProvider(context, basicUi), HostProviderPriority.Low);

                // Run!
                int exitCode = app.RunAsync(args)
                                  .ConfigureAwait(false)
                                  .GetAwaiter()
                                  .GetResult();

                Environment.Exit(exitCode);
            }
        }

        private static string GetApplicationPath()
        {
            // Assembly::Location always returns an empty string if the application was published as a single file
#pragma warning disable IL3000
            bool isSingleFile = string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location);
#pragma warning restore IL3000

            // Use "argv[0]" to get the full path to the entry executable - this is consistent across
            // .NET Framework and .NET >= 5 when published as a single file.
            string[] args = Environment.GetCommandLineArgs();
            string candidatePath = args[0];

            // If we have not been published as a single file on .NET 5 then we must strip the ".dll" file extension
            // to get the default AppHost/SuperHost name.
            if (!isSingleFile && Path.HasExtension(candidatePath))
            {
                return Path.ChangeExtension(candidatePath, null);
            }

            return candidatePath;
        }

        private static bool TryFindHelperExecutablePath(
            ICommandContext context, string envar, string configName, out string path)
        {
            path = null;

            if (!context.Settings.TryGetSetting(
                envar, Constants.GitConfiguration.Credential.SectionName, configName, out string helperName))
            {
                return false;
            }

            context.Trace.WriteLine($"UI helper override specified: '{helperName}'.");

            // If the user set the helper override to the empty string then they are signalling not to use a helper
            if (string.IsNullOrEmpty(helperName))
            {
                return false;
            }

            if (Path.IsPathRooted(helperName))
            {
                path = helperName;
            }
            else
            {
                string executableDirectory = Path.GetDirectoryName(context.ApplicationPath);
                path = Path.Combine(executableDirectory!, helperName);
            }

            if (!context.FileSystem.FileExists(path))
            {
                context.Trace.WriteLine($"UI helper '{helperName}' was not found at '{path}'.");
                context.Streams.Error.WriteLine($"warning: could not find configured UI helper '{helperName}'");

                return false;
            }

            return true;
        }
    }
}
