using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication;

namespace Microsoft.AzureRepos
{
    public interface IAzureDevOpsAuthentication
    {
        Task<AzureDevOpsSelectAccountResult> SelectAccountAsync(IEnumerable<IMicrosoftAccount> accounts);
    }

    public class AzureDevOpsSelectAccountResult
    {
        public IMicrosoftAccount Account { get; set; }

        public bool UseForAllOrgs { get; set; }
    }

    public class AzureDevOpsAuthentication : AuthenticationBase, IAzureDevOpsAuthentication
    {
        public AzureDevOpsAuthentication(ICommandContext context)
            : base(context) { }

        public async Task<AzureDevOpsSelectAccountResult> SelectAccountAsync(IEnumerable<IMicrosoftAccount> accounts)
        {
            IMicrosoftAccount[] accountsArray = accounts.ToArray();
            if (accountsArray.Length == 0)
            {
                return null;
            }

            ThrowIfUserInteractionDisabled();

            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession &&
                TryFindHelperExecutablePath(out string path))
            {
                var accountLines = new List<string>();

                foreach (IMicrosoftAccount account in accountsArray)
                {
                    var sb = new StringBuilder(account.UserName);
                    if (!string.IsNullOrWhiteSpace(account.DisplayName))
                    {
                        sb.AppendFormat(";{0}", account.DisplayName);
                    }
                    else
                    {
                        sb.Append(';');
                    }

                    if (account.IsPersonalAccount)
                    {
                        sb.Append(";true");
                    }

                    accountLines.Add(sb.ToString());
                }

                var inputDict = new Dictionary<string, IList<string>>
                {
                    ["account"] = accountLines
                };

                IDictionary<string, string> resultDict = await InvokeHelperAsync(path, "select-account", inputDict);

                if (resultDict.TryGetValue("new", out string newStr) && newStr.IsTruthy())
                {
                    return null;
                }

                if (!resultDict.TryGetValue("account", out string selectedAccount))
                {
                    throw new Exception("Missing 'account' in response");
                }

                resultDict.TryGetValue("allorgs", out string useForAllOrgs);

                IMicrosoftAccount act = accountsArray.First(x => StringComparer.Ordinal.Equals(x.UserName, selectedAccount));

                return new AzureDevOpsSelectAccountResult
                {
                    Account = act,
                    UseForAllOrgs = useForAllOrgs.IsTruthy()
                };
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                var actMenu = new TerminalMenu(Context.Terminal, "Select an account");
                TerminalMenuItem newActItem = actMenu.Add("Add new account");
                var actItemMap = new Dictionary<TerminalMenuItem, IMicrosoftAccount>
                {
                    [newActItem] = null // null => new account
                };

                foreach (IMicrosoftAccount account in accountsArray)
                {
                    string type = account.IsPersonalAccount ? "Personal Account" : "Work or School Account";
                    string displayName = $"{account.UserName} ({type})";
                    var actItem = actMenu.Add(displayName);
                    actItemMap[actItem] = account;
                }

                TerminalMenuItem actChoice = actMenu.Show();
                IMicrosoftAccount act = actItemMap[actChoice];

                var allMenu = new TerminalMenu(Context.Terminal, "Use this account for all organizations?");
                var yesItem = allMenu.Add("Yes");
                var noItem = allMenu.Add("No");
                var allChoice = allMenu.Show(0);

                bool useForAllOrgs = allChoice == yesItem;

                return new AzureDevOpsSelectAccountResult{Account = act, UseForAllOrgs = useForAllOrgs};
            }
        }

        private bool TryFindHelperExecutablePath(out string path)
        {
            return TryFindHelperExecutablePath(
                AzureDevOpsConstants.EnvironmentVariables.AuthenticationHelper,
                AzureDevOpsConstants.GitConfiguration.Credential.AuthenticationHelper,
                AzureDevOpsConstants.DefaultAuthenticationHelper,
                out path);
        }
    }
}
