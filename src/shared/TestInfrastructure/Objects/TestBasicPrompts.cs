// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestBasicPrompts : IBasicPrompts
    {
        public Func<string, string, ICredential> CredentialPrompt { get; set; } = (resource, user) => null;

        public Task<ICredential> ShowCredentialPromptAsync(string resource, string userName)
        {
            return Task.FromResult(CredentialPrompt?.Invoke(resource, userName));
        }
    }
}
