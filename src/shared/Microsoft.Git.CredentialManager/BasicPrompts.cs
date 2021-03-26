// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    public interface IBasicPrompts
    {
        Task<ICredential> ShowCredentialPromptAsync(string resource, string userName);
    }

    public static class BasicPromptsExtensions
    {
        public static Task<ICredential> ShowCredentialPromptAsync(this IBasicPrompts basicAuth, string resource)
        {
            return basicAuth.ShowCredentialPromptAsync(resource, null);
        }
    }

    public class BasicTerminalPrompts : TerminalPrompts, IBasicPrompts
    {
        public BasicTerminalPrompts(ICommandContext context)
            : base (context.Settings, context.Terminal) { }

        public Task<ICredential> ShowCredentialPromptAsync(string resource, string userName)
        {
            EnsureArgument.NotNullOrWhiteSpace(resource, nameof(resource));

            ThrowIfTerminalPromptsDisabled();

            Terminal.WriteLine("Enter basic credentials for '{0}':", resource);

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

            return Task.FromResult((ICredential) new GitCredential(userName, password));
        }
    }
}
