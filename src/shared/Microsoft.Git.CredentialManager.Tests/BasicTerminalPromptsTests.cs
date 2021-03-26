// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class BasicTerminalPromptsTests
    {
        [Fact]
        public void BasicTerminalPrompts_ShowCredentialPromptAsync_NullResource_ThrowsException()
        {
            var context = new TestCommandContext();
            var basicAuth = new BasicTerminalPrompts(context);

            Assert.ThrowsAsync<ArgumentNullException>(() => basicAuth.ShowCredentialPromptAsync(null));
        }

        [Fact]
        public async Task BasicTerminalPrompts_ShowCredentialPromptAsync_NonDesktopSession_ResourceAndUserName_PasswordPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var context = new TestCommandContext {SessionManager = {IsDesktopSession = false}};
            context.Terminal.SecretPrompts["Password"] = testPassword; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var basicPrompt = new BasicTerminalPrompts(context);

            ICredential credential = await basicPrompt.ShowCredentialPromptAsync(testResource, testUserName);

            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public async Task BasicTerminalPrompts_ShowCredentialPromptAsync_NonDesktopSession_Resource_UserPassPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var context = new TestCommandContext {SessionManager = {IsDesktopSession = false}};
            context.Terminal.Prompts["Username"] = testUserName;
            context.Terminal.SecretPrompts["Password"] = testPassword; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var basicPrompt = new BasicTerminalPrompts(context);

            ICredential credential = await basicPrompt.ShowCredentialPromptAsync(testResource);

            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public void BasicTerminalPrompts_ShowCredentialPromptAsync_NonDesktopSession_NoTerminalPrompts_ThrowsException()
        {
            const string testResource = "https://example.com";

            var context = new TestCommandContext
            {
                SessionManager = {IsDesktopSession = false},
                Settings = {IsInteractionAllowed = false},
            };

            var basicPrompt = new BasicTerminalPrompts(context);

            Assert.ThrowsAsync<InvalidOperationException>(() => basicPrompt.ShowCredentialPromptAsync(testResource));
        }
    }
}
