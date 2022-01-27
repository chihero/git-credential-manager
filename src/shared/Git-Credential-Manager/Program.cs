using System;
using Atlassian.Bitbucket;
using GitHub;
using GitLab;
using Microsoft.AzureRepos;
using GitCredentialManager.Authentication;

namespace GitCredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string appPath = ApplicationBase.GetEntryApplicationPath();
            using (var context = new CommandContext(appPath))
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

                // Warn about being invoked as the old "git-credential-manager-core" name
                if (IsOldExecutableName())
                {
                    context.Streams.Error.WriteLine("warning: git-credential-manager was invoked with an older alias." +
                                                    Environment.NewLine +
                                                    $"Please see {Constants.HelpUrls.GcmOldExeName} for more information.");
                }

                // Register all supported host providers at the normal priority.
                // The generic provider should never win against a more specific one, so register it with low priority.
                app.RegisterProvider(new AzureReposHostProvider(context), HostProviderPriority.Normal);
                app.RegisterProvider(new BitbucketHostProvider(context),  HostProviderPriority.Normal);
                app.RegisterProvider(new GitHubHostProvider(context),     HostProviderPriority.Normal);
                app.RegisterProvider(new GitLabHostProvider(context),     HostProviderPriority.Normal);
                app.RegisterProvider(new GenericHostProvider(context),    HostProviderPriority.Low);

                int exitCode = app.RunAsync(args)
                                  .ConfigureAwait(false)
                                  .GetAwaiter()
                                  .GetResult();

                Environment.Exit(exitCode);
            }
        }

        private static bool IsOldExecutableName()
        {
            string argv0;
            try
            {
                argv0 = PlatformUtils.GetNativeArgv0();
            }
            catch
            {
                // We don't want to crash just because we failed to get argv[0] to show a warning message!
                // Optimistically return false and assume we're being called using the new executable name.
                return false;
            }

            string exe = PlatformUtils.IsWindows() ? ".exe" : string.Empty;

            return argv0.EndsWith($"git-credential-manager-core{exe}",
                PlatformUtils.IsLinux() ?
                    StringComparison.Ordinal :
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
