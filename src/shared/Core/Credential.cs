
using System;
using System.Collections.Generic;

namespace GitCredentialManager
{
    /// <summary>
    /// Represents a credential.
    /// </summary>
    public interface ICredential
    {
        /// <summary>
        /// Account associated with this credential.
        /// </summary>
        string Account { get; }

        /// <summary>
        /// Password.
        /// </summary>
        string Password { get; }
    }

    /// <summary>
    /// Represents a credential (username/password pair) that Git can use to authenticate to a remote repository.
    /// </summary>
    public class GitCredential : ICredential
    {
        public GitCredential(string userName, string password)
        {
            Account = userName;
            Password = password;
        }

        public string Account { get; }

        public string Password { get; }

        public IDictionary<string, string> AdditionalProperties { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static class CredentialExtensions
    {
        public static GitCredential AsGitCredential(this ICredential credential)
        {
            if (credential is GitCredential gitCredential)
            {
                return gitCredential;
            }

            return new GitCredential(credential.Account, credential.Password);
        }
    }
}
