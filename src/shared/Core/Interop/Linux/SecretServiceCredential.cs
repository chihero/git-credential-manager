using System.Diagnostics;

namespace GitCredentialManager.Interop.Linux
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class SecretServiceCredential : ICredential
    {
        internal SecretServiceCredential(string service, string account, string password)
        {
            Service = service;
            Account = account;
            Secret = password;
        }

        public string Service { get; }

        public string Account { get; }

        public string Secret { get; }

        private string DebuggerDisplay => $"[Service: {Service}, Account: {Account}]";
    }
}
