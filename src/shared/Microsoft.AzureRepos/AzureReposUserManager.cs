using System.Collections.Generic;

namespace Microsoft.AzureRepos
{
    public interface IAzureReposUserManager
    {
        IEnumerable<AzureReposUserBinding> GetUserBindings();
    }

    public abstract class AzureReposUserBinding
    {
        protected AzureReposUserBinding(string userName)
        {
            UserName = userName;
        }

        public string UserName { get; set; }
    }

    public class AzureReposOrgBinding : AzureReposUserBinding
    {
        public AzureReposOrgBinding(string organization, string userName)
            : base(userName)
        {
            Organization = organization;
        }

        public string Organization { get; set; }
    }

    public class AzureReposAuthorityBinding : AzureReposUserBinding
    {
        public AzureReposAuthorityBinding(string authority, string userName)
            : base(userName)
        {
            Authority = authority;
        }

        public string Authority { get; set; }
    }

    public class AzureReposUserManager : IAzureReposUserManager
    {
        public IEnumerable<AzureReposUserBinding> GetUserBindings()
        {
            yield return new AzureReposAuthorityBinding(
                "login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47", "mattche@microsoft.com");

            yield return new AzureReposOrgBinding(
                "mseng", "johasc@microsoft.com");
        }
    }
}
