using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketAuthenticationTest
    {
        [Theory]
        [InlineData("jsquire", "password")]
        public async Task BitbucketAuthentication_GetCredentialsAsync_Basic_SucceedsAfterUserInput(string username, string password)
        {
            var context = new TestCommandContext();
            context.Terminal.Prompts["Username"] = username;
            context.Terminal.SecretPrompts["Password"] = password;
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, username, AuthenticationModes.Basic);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(username, result.Credential.Account);
            Assert.Equal(password, result.Credential.Secret);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_OAuth_ReturnsOAuth()
        {
            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Allow OAuth mode
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, null, AuthenticationModes.OAuth);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.OAuth, result.AuthenticationMode);
            Assert.Null(result.Credential);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_All_ShowsMenu_OAuthOption1()
        {
            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Allow OAuth mode
            context.Terminal.Prompts["option (enter for default)"] = "1";
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.OAuth, result.AuthenticationMode);
            Assert.Null(result.Credential);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_All_ShowsMenu_BasicOption2()
        {
            const string username = "jsquire";
            const string password = "password";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Allow OAuth mode
            context.Terminal.Prompts["option (enter for default)"] = "2";
            context.Terminal.Prompts["Username"] = username;
            context.Terminal.SecretPrompts["Password"] = password;
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(username, result.Credential.Account);
            Assert.Equal(password, result.Credential.Secret);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_All_NoDesktopSession_BasicOnly()
        {
            const string username = "jsquire";
            const string password = "password";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = false; // Disallow OAuth mode
            context.Terminal.Prompts["Username"] = username;
            context.Terminal.SecretPrompts["Password"] = password;
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(username, result.Credential.Account);
            Assert.Equal(password, result.Credential.Secret);
        }

        [Fact]
        public async Task BitbucketAuthentication_ShowOAuthRequiredPromptAsync_SucceedsAfterUserInput()
        {
            var context = new TestCommandContext();
            context.Terminal.Prompts["Press enter to continue..."] = " ";

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.ShowOAuthRequiredPromptAsync();

            Assert.True(result);
            Assert.Equal($"Your account has two-factor authentication enabled.{Environment.NewLine}" +
                                           $"To continue you must complete authentication in your web browser.{Environment.NewLine}", context.Terminal.Messages[0].Item1);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_AllModes_NoUser_BBCloud_HelperCmdLine()
        {
            var targetUri = new Uri("https://bitbucket.org");

            var helperPath = "/usr/bin/test-helper";
            var expectedUserName = "jsquire";
            var expectedPassword = "password";
            var resultDict = new Dictionary<string, string>
            {
                ["username"] = expectedUserName,
                ["password"] = expectedPassword
            };

            string expectedArgs = $"userpass --show-oauth";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Enable OAuth and UI helper selection

            var authMock = new Mock<BitbucketAuthentication>(context) { CallBase = true };
            authMock.Setup(x => x.TryFindHelperExecutablePath(out helperPath))
                .Returns(true);
            authMock.Setup(x => x.InvokeHelperAsync(It.IsAny<string>(), It.IsAny<string>(), null, CancellationToken.None))
                .ReturnsAsync(resultDict);

            BitbucketAuthentication auth = authMock.Object;
            CredentialsPromptResult result = await auth.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(result.Credential.Account, expectedUserName);
            Assert.Equal(result.Credential.Secret, expectedPassword);

            authMock.Verify(x => x.InvokeHelperAsync(helperPath, expectedArgs, null, CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_BasicOnly_User_BBCloud_HelperCmdLine()
        {
            var targetUri = new Uri("https://bitbucket.org");

            var helperPath = "/usr/bin/test-helper";
            var expectedUserName = "jsquire";
            var expectedPassword = "password";
            var resultDict = new Dictionary<string, string>
            {
                ["username"] = expectedUserName,
                ["password"] = expectedPassword
            };

            string expectedArgs = $"userpass --username {expectedUserName}";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Enable UI helper selection

            var authMock = new Mock<BitbucketAuthentication>(context) { CallBase = true };
            authMock.Setup(x => x.TryFindHelperExecutablePath(out helperPath))
                .Returns(true);
            authMock.Setup(x => x.InvokeHelperAsync(It.IsAny<string>(), It.IsAny<string>(), null, CancellationToken.None))
                .ReturnsAsync(resultDict);

            BitbucketAuthentication auth = authMock.Object;
            CredentialsPromptResult result = await auth.GetCredentialsAsync(targetUri, expectedUserName, AuthenticationModes.Basic);

            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(result.Credential.Account, expectedUserName);
            Assert.Equal(result.Credential.Secret, expectedPassword);

            authMock.Verify(x => x.InvokeHelperAsync(helperPath, expectedArgs, null, CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_AllModes_NoUser_BBServerDC_HelperCmdLine()
        {
            var targetUri = new Uri("https://example.com/bitbucket");

            var helperPath = "/usr/bin/test-helper";
            var expectedUserName = "jsquire";
            var expectedPassword = "password";
            var resultDict = new Dictionary<string, string>
            {
                ["username"] = expectedUserName,
                ["password"] = expectedPassword
            };

            string expectedArgs = $"userpass --url {targetUri} --show-oauth";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Enable OAuth and UI helper selection

            var authMock = new Mock<BitbucketAuthentication>(context) { CallBase = true };
            authMock.Setup(x => x.TryFindHelperExecutablePath(out helperPath))
                .Returns(true);
            authMock.Setup(x => x.InvokeHelperAsync(It.IsAny<string>(), It.IsAny<string>(), null, CancellationToken.None))
                .ReturnsAsync(resultDict);

            BitbucketAuthentication auth = authMock.Object;
            CredentialsPromptResult result = await auth.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(result.Credential.Account, expectedUserName);
            Assert.Equal(result.Credential.Secret, expectedPassword);

            authMock.Verify(x => x.InvokeHelperAsync(helperPath, expectedArgs, null, CancellationToken.None),
                Times.Once);
        }
    }
}
