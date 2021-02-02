// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class AzureReposAuthorityCacheTests
    {
        [Fact]
        public void AzureReposAuthorityCache_GetAuthority_Null_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            Assert.ThrowsAsync<ArgumentNullException>(() => cache.GetAuthorityAsync(null));
        }

        [Fact]
        public async Task AzureReposAuthorityCache_GetAuthority_NoCachedAuthority_ReturnsNull()
        {
            const string key = "org.contoso.authority";

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            string authority = await cache.GetAuthorityAsync(key);

            Assert.Null(authority);
        }

        [Fact]
        public async Task AzureReposAuthorityCache_GetAuthority_CachedAuthority_ReturnsAuthority()
        {
            const string orgName = "contoso";
            const string key = "org.contoso.authority";
            const string expectedAuthority = "https://login.contoso.com";

            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = expectedAuthority
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            string actualAuthority = await cache.GetAuthorityAsync(orgName);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureReposAuthorityCache_GetAuthority_CachedAuthority_PersistedStoreChanged_ReturnsPersistedAuthority()
        {
            const string orgName = "contoso";
            const string key = "org.contoso.authority";
            const string oldAuthority = "https://old-login.contoso.com";
            const string expectedAuthority = "https://login.contoso.com";

            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = oldAuthority
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            // Update persisted store after creation of the authority cache
            store.PersistedStore[key] = expectedAuthority;
            // The in-memory value should be stale
            Assert.Equal(oldAuthority, store.MemoryStore[key]);

            string actualAuthority = await cache.GetAuthorityAsync(orgName);

            // Should have reloaded from the persisted store
            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureReposAuthorityCache_UpdateAuthority_NoCachedAuthority_SetsAuthorityInPersistedStore()
        {
            const string orgName = "contoso";
            const string key = "org.contoso.authority";
            const string expectedAuthority = "https://login.contoso.com";

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            await cache.UpdateAuthorityAsync(orgName, expectedAuthority);

            Assert.True(store.PersistedStore.TryGetValue(key, out string actualAuthority));
            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureReposAuthorityCache_UpdateAuthority_CachedAuthority_UpdatesAuthority()
        {
            const string orgName = "contoso";
            const string key = "org.contoso.authority";
            const string oldAuthority = "https://old-login.contoso.com";
            const string expectedAuthority = "https://login.contoso.com";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = oldAuthority
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            await cache.UpdateAuthorityAsync(orgName, expectedAuthority);

            Assert.True(store.PersistedStore.TryGetValue(key, out string actualAuthority));
            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureReposAuthorityCache_UpdateAuthority_CachedAuthority_PersistedStoreChanged_OverwritesPersistedAuthority()
        {
            const string orgName = "contoso";
            const string key = "org.contoso.authority";
            const string otherAuthority = "https://alt-login.contoso.com";
            const string expectedAuthority = "https://login.contoso.com";

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            // Persisted store is updated after the authority cache is created
            store.PersistedStore[key] = otherAuthority;

            await cache.UpdateAuthorityAsync(orgName, expectedAuthority);

            Assert.True(store.PersistedStore.TryGetValue(key, out string actualAuthority));
            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureReposAuthorityCache_EraseAuthority_NoCachedAuthority_DoesNothing()
        {
            const string orgName = "contoso";
            const string key = "org.contoso.authority";
            const string otherKey = "org.fabrikam.authority";
            const string otherAuthority = "https://fabrikam.com/login";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [otherKey] = otherAuthority
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            await cache.EraseAuthorityAsync(orgName);

            // Other entries should remain in the persisted store
            Assert.False(store.PersistedStore.ContainsKey(key));
            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(otherKey, out string actualOtherAuthority));
            Assert.Equal(otherAuthority, actualOtherAuthority);
        }

        [Fact]
        public async Task AzureReposAuthorityCache_EraseAuthority_CachedAuthority_RemovesAuthority()
        {
            const string orgName = "contoso";
            const string key = "org.contoso.authority";
            const string authority = "https://login.contoso.com";
            const string otherKey = "org.fabrikam.authority";
            const string otherAuthority = "https://fabrikam.com/login";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = authority,
                [otherKey] = otherAuthority
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            await cache.EraseAuthorityAsync(orgName);

            // Only the other entries should remain in the persisted store
            Assert.False(store.PersistedStore.ContainsKey(key));
            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(otherKey, out string actualOtherAuthority));
            Assert.Equal(otherAuthority, actualOtherAuthority);
        }

        [Fact]
        public async Task AzureReposAuthorityCache_Clear_CachedAuthorities_RemovesAllAuthorities()
        {
            const string key1 = "org.contoso.authority";
            const string key2 = "org.fabrikam.authority";
            const string authority1 = "https://login.contoso.com";
            const string authority2 = "https://login.fabrikam.com";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [key1] = authority1,
                [key2] = authority2
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureDevOpsAuthorityCache(trace, store);

            await cache.ClearAsync();

            // No entries should be left in the persisted store
            Assert.Empty(store.PersistedStore);
        }
    }
}
