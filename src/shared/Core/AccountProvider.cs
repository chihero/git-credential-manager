using System.Collections.Generic;

namespace GitCredentialManager
{
    public interface IAccountProvider
    {
        IAccount GetAccount(InputArguments input);

        IEnumerable<IAccount> GetAccounts();

        void SetAccount(InputArguments input, IAccount account);
    }

    public interface IAccount
    {

    }

    public class AccountProvider : IAccountProvider
    {
        private readonly ITrace _trace;
        private readonly IGit _git;

        public AccountProvider(ITrace trace, IGit git)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(git, nameof(git));

            _trace = trace;
            _git = git;
        }

        public IAccount GetAccount(InputArguments input)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<IAccount> GetAccounts()
        {
            throw new System.NotImplementedException();
        }

        public void SetAccount(InputArguments input, IAccount account)
        {
            throw new System.NotImplementedException();
        }
    }
}
