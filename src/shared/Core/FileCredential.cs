namespace GitCredentialManager
{
    public class FileCredential : ICredential
    {
        public FileCredential(string fullPath, string service, string account, string secret)
        {
            FullPath = fullPath;
            Service = service;
            Account = account;
            Secret = secret;
        }

        public string FullPath { get; }

        public string Service { get; }

        public string Account { get; }

        public string Secret { get; }
    }
}
