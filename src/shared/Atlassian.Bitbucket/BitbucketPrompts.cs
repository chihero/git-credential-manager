// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;

namespace Atlassian.Bitbucket
{
    public interface IBitbucketPrompts
    {
        Task<BasicPromptResult> ShowBasicPromptAsync(Uri targetUri, string userName);
        Task<OAuthPromptResult> ShowOAuthPromptAsync();
    }

    public class BasicPromptResult
    {
        public ICredential Credential { get; set; }
    }

    public class OAuthPromptResult
    {
        public bool Continue { get; set; }
    }

    public class BitbucketTerminalPrompts : TerminalPrompts, IBitbucketPrompts
    {
        public BitbucketTerminalPrompts(ICommandContext context)
            : base(context.Settings, context.Terminal) { }

        public BitbucketTerminalPrompts(ISettings settings, ITerminal terminal)
            : base(settings, terminal) { }

        public Task<BasicPromptResult> ShowBasicPromptAsync(Uri targetUri, string userName)
        {
            ThrowIfTerminalPromptsDisabled();

            Terminal.WriteLine("Enter Bitbucket credentials for '{0}'...", targetUri);

            if (!string.IsNullOrWhiteSpace(userName))
            {
                // Don't need to prompt for the username if it has been specified already
                Terminal.WriteLine("Username: {0}", userName);
            }
            else
            {
                // Prompt for username
                userName = Terminal.Prompt("Username");
            }

            // Prompt for password
            string password = Terminal.PromptSecret("Password");

            return Task.FromResult(new BasicPromptResult
            {
                Credential = new GitCredential(userName, password)
            });
        }

        public Task<OAuthPromptResult> ShowOAuthPromptAsync()
        {
            ThrowIfTerminalPromptsDisabled();

            Terminal.WriteLine($"Your account has two-factor authentication enabled.{Environment.NewLine}" +
                               $"To continue you must complete authentication in your web browser.{Environment.NewLine}");

            var _ = Terminal.Prompt("Press enter to continue...");

            return Task.FromResult(new OAuthPromptResult
            {
                Continue = true
            });
        }
    }
}
