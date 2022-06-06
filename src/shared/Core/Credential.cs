
namespace GitCredentialManager
{
    /// <summary>
    /// Represents metadata/attributes about a credential.
    /// </summary>
    public interface ICredentialAttributes
    {
        /// <summary>
        /// Account associated with this credential.
        /// </summary>
        public string Account { get; }
    }

    /// <summary>
    /// Represents a credential. A credential consists of attributes/metadata and a secret value.
    /// </summary>
    public interface ICredential : ICredentialAttributes
    {
        /// <summary>
        /// Secret.
        /// </summary>
        string Secret { get; }
    }

    /// <summary>
    /// Represents a credential (username/password pair) that Git can use to authenticate to a remote repository.
    /// </summary>
    public class GitCredential : ICredential
    {
        public GitCredential(string account, string secret)
        {
            Account = account;
            Secret = secret;
        }

        public string Account { get; }

        public string Secret { get; }
    }
}
