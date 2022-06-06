
namespace GitCredentialManager.Interop.Windows
{
    public class WindowsCredential : ICredential
    {
        public WindowsCredential(string service, string userName, string password, string targetName)
        {
            Service = service;
            UserName = userName;
            Secret = password;
            TargetName = targetName;
        }

        public string Service { get; }

        public string UserName { get; }

        public string Secret { get; }

        public string TargetName { get; }

        string ICredential.Account => UserName;
    }
}
