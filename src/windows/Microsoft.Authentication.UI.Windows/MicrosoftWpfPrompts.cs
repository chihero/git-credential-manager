// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using Microsoft.Authentication.UI.Login;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Identity.Client;

namespace Microsoft.Authentication.UI
{
    public class MicrosoftWpfPrompts : PromptsBase , IMicrosoftPrompts
    {
        private readonly IGui _gui;

        public MicrosoftWpfPrompts(ISettings settings, IGui gui) : base(settings)
        {
            EnsureArgument.NotNull(gui, nameof(gui));

            _gui = gui;
        }

        public Task ShowDeviceCodeAsync(DeviceCodeResult deviceCodeResult)
        {
            ThrowIfUserInteractionDisabled();

            var viewModel = new LoginDeviceViewModel(deviceCodeResult.UserCode, new Uri(deviceCodeResult.VerificationUrl));

            bool valid = _gui.ShowDialogWindow(viewModel, () => new LoginDeviceView());

            return Task.CompletedTask;
        }
    }
}
