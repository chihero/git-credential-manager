using System;

namespace Microsoft.AzureRepos
{
    internal static class AzureDevOpsConstants
    {
        // AAD environment authority base URL
        public const string AadAuthorityBaseUrl = "https://login.microsoftonline.com";

        public static readonly string[] AzureDevOpsDefaultScopes =
        {
            OAuthScopes.ReposFull, OAuthScopes.ArtifactsRead
        };

        public const string AzDevClientId = "499b84ac-1321-427f-aa17-267ca6975798";
        public const string AzDevAppUrl = "https://app.vssps.visualstudio.com";
        public const string VsClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";

        public const string GcmClientId = "d735b71b-9eee-4a4f-ad23-421660877ba6";
        public static readonly Uri GcmRedirectUri = new Uri("http://localhost");

        public const string VstsHostSuffix = ".visualstudio.com";
        public const string AzureDevOpsHost = "dev.azure.com";

        public const string VssResourceTenantHeader = "X-VSS-ResourceTenant";

        public const string PatCredentialType = "pat";
        public const string OAuthCredentialType = "oauth";

        public const string UrnScheme = "azrepos";
        public const string UrnOrgPrefix = "org";

        public static class PersonalAccessTokenScopes
        {
            public const string ReposWrite = "vso.code_write";
            public const string ArtifactsRead = "vso.packaging";
        }

        public static class OAuthScopes
        {
            public const string UserImpersonation = $"{AzDevAppUrl}/.default";
            public const string ReposFull = $"{AzDevAppUrl}/vso.code_full";
            public const string ArtifactsRead = $"{AzDevAppUrl}/vso.packaging";
        }

        public static class EnvironmentVariables
        {
            public const string DevAadClientId = "GCM_DEV_AZREPOS_CLIENTID";
            public const string DevAadRedirectUri = "GCM_DEV_AZREPOS_REDIRECTURI";
            public const string DevAadAuthorityBaseUri = "GCM_DEV_AZREPOS_AUTHORITYBASEURI";
            public const string CredentialType = "GCM_AZREPOS_CREDENTIALTYPE";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string DevAadClientId = "azreposDevClientId";
                public const string DevAadRedirectUri = "azreposDevRedirectUri";
                public const string DevAadAuthorityBaseUri = "azreposDevAuthorityBaseUri";
                public const string CredentialType = "azreposCredentialType";
                public const string AzureAuthority = "azureAuthority";
            }
        }
    }
}
