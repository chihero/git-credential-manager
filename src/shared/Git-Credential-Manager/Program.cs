using System;
using System.Threading;
using Atlassian.Bitbucket;
using GitHub;
using GitLab;
using Microsoft.AzureRepos;
using GitCredentialManager.Authentication;
using GitCredentialManager.UI;

namespace GitCredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // Create the dispatcher on the main thread. This is required
            // for some platform UI services such as macOS that mandates
            // all controls are created/accessed on the initial thread
            // created by the process (the process entry thread).
            Dispatcher.Initialize();

            // Run AppMain in a new thread and keep the main thread free
            // to process the dispatcher's job queue.
            var appMain = new Thread(AppMain) {Name = nameof(AppMain)};
            appMain.Start(args);

            // Process the dispatcher job queue (aka: message pump, run-loop, etc...)
            // We must ensure to run this on the same thread that it was created on
            // (the main thread) so we cannot use any async/await calls between
            // Dispatcher.Initialize and Run.
            Dispatcher.MainThread.Run();
        }

        private static void AppMain(object o)
        {
            string[] args = (string[]) o;

            string appPath = ApplicationBase.GetEntryApplicationPath();
            string installDir = ApplicationBase.GetInstallationDirectory();
            using (var context = new CommandContext(appPath, installDir))
            using (var app = new Application(context))
            {
                // Workaround for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2560
                if (MicrosoftAuthentication.CanUseBroker(context))
                {
                    try
                    {
                        MicrosoftAuthentication.InitializeBroker();
                    }
                    catch (Exception ex)
                    {
                        context.Streams.Error.WriteLine(
                            "warning: broker initialization failed{0}{1}",
                            Environment.NewLine, ex.Message
                        );
                    }
                }

                //
                // Git Credential Manager's executable used to be named "git-credential-manager-core" before
                // dropping the "-core" suffix. In order to prevent "helper not found" errors for users who
                // haven't updated their configuration, we include either a 'shim' or symlink with the old name
                // that print warning messages about using the old name, and then continue execution of GCM.
                //
                // On Windows the shim is an exact copy of the main "git-credential-manager.exe" executable
                // with the old name. We inspect argv[0] to see which executable we are launched as.
                //
                // On UNIX systems we do the same check, except instead of a copy we use a symlink.
                //
                string oldName = PlatformUtils.IsWindows()
                    ? "git-credential-manager-core.exe"
                    : "git-credential-manager-core";

                if (appPath?.EndsWith(oldName, StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    context.Streams.Error.WriteLine(
                        "warning: git-credential-manager-core was renamed to git-credential-manager");
                    context.Streams.Error.WriteLine(
                        $"warning: see {Constants.HelpUrls.GcmExecRename} for more information");
                }

                // Register all supported host providers at the normal priority.
                // The generic provider should never win against a more specific one, so register it with low priority.
                app.RegisterProvider(new AzureReposHostProvider(context), HostProviderPriority.Normal);
                app.RegisterProvider(new BitbucketHostProvider(context),  HostProviderPriority.Normal);
                app.RegisterProvider(new GitHubHostProvider(context),     HostProviderPriority.Normal);
                app.RegisterProvider(new GitLabHostProvider(context),     HostProviderPriority.Normal);
                app.RegisterProvider(new GenericHostProvider(context),    HostProviderPriority.Low);

                Environment.ExitCode = app.RunAsync(args)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                // Shutdown any UI run-loops
                AvaloniaUi.Shutdown();

                // Stop our main message loop
                Dispatcher.MainThread.Shutdown();
            }
        }
    }
}
