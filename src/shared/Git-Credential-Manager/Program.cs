using System;
using Atlassian.Bitbucket;
using GitHub;
using GitLab;
using Microsoft.AzureRepos;

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
    }
}
