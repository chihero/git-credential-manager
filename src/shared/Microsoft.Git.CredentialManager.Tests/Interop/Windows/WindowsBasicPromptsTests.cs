// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Git.CredentialManager.Interop.Windows;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Interop.Windows
{
    public class WindowsBasicPromptsTests
    {
        [Fact]
        public void WindowsSystemPrompts_ShowCredentialPrompt_NullResource_ThrowsException()
        {
            var prompts = new WindowsBasicPrompts(new TestSettings());
            Assert.ThrowsAsync<ArgumentNullException>(() => prompts.ShowCredentialPromptAsync(null, null));
        }

        [Fact]
        public void WindowsSystemPrompts_ShowCredentialPrompt_EmptyResource_ThrowsException()
        {
            var prompts = new WindowsBasicPrompts(new TestSettings());
            Assert.ThrowsAsync<ArgumentException>(() => prompts.ShowCredentialPromptAsync(string.Empty, null));
        }

        [Fact]
        public void WindowsSystemPrompts_ShowCredentialPrompt_WhiteSpaceResource_ThrowsException()
        {
            var prompts = new WindowsBasicPrompts(new TestSettings());
            Assert.ThrowsAsync<ArgumentException>(() => prompts.ShowCredentialPromptAsync("   ", null));
        }
    }
}
