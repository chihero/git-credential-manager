using System;

namespace Microsoft.AzureRepos
{
    internal static class AzureDevOpsConstants
    {
        // AAD environment authority base URL
        public const string AadAuthorityBaseUrl = "https://login.microsoftonline.com";

        public static class OAuthTokenScopes
        {
            public const string ReposFull = "vso.code_full";
            public const string ArtifactsRead = "vso.packaging";
        }

        public static readonly string[] AzureDevOpsOAuthScopes =
        {
            $"{AzureDevOpsAppId}/{OAuthTokenScopes.ReposFull}",
            $"{AzureDevOpsAppId}/{OAuthTokenScopes.ArtifactsRead}",
        };

        public const string AzureDevOpsAppId = "499b84ac-1321-427f-aa17-267ca6975798";
        public const string GitCredentialManagerAppId = "d735b71b-9eee-4a4f-ad23-421660877ba6";

        public const string AadClientId = GitCredentialManagerAppId;
        public static readonly Uri AadRedirectUri = new Uri("http://localhost");

        public const string VstsHostSuffix = ".visualstudio.com";
        public const string AzureDevOpsHost = "dev.azure.com";

        public const string VssResourceTenantHeader = "X-VSS-ResourceTenant";

        public const string UrnScheme = "azrepos";
        public const string UrnOrgPrefix = "org";

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
                public const string AzureAuthority = "azureAuthority";
            }
        }
    }
}
