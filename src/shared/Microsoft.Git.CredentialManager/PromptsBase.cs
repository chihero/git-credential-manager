// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager
{
    public abstract class PromptsBase
    {
        protected readonly ISettings Settings;

        protected PromptsBase(ISettings settings)
        {
            EnsureArgument.NotNull(settings, nameof(settings));

            Settings = settings;
        }

        protected void ThrowIfUserInteractionDisabled()
        {
            if (!Settings.IsInteractionAllowed)
            {
                string envName = Constants.EnvironmentVariables.GcmInteractive;
                string cfgName = string.Format("{0}.{1}",
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.Interactive);

                throw new InvalidOperationException(
                    $"Cannot prompt because user interactivity has been disabled ({envName} or {cfgName} is false/never).");
            }
        }
    }

    public abstract class TerminalPrompts : PromptsBase
    {
        protected readonly ITerminal Terminal;

        protected TerminalPrompts(ISettings settings, ITerminal terminal) : base (settings)
        {
            EnsureArgument.NotNull(terminal, nameof(terminal));

            Terminal = terminal;
        }

        protected void ThrowIfTerminalPromptsDisabled()
        {
            if (!Settings.IsTerminalPromptsEnabled)
            {
                throw new InvalidOperationException(
                    $"Cannot prompt because terminal prompts have been disabled ({Constants.EnvironmentVariables.GitTerminalPrompts} is 0).");
            }

            ThrowIfUserInteractionDisabled();
        }
    }
}
