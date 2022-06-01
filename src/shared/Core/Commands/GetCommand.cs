using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitCredentialManager.Commands
{
    /// <summary>
    /// Acquire a new <see cref="GitCredential"/> from a <see cref="IHostProvider"/>.
    /// </summary>
    public class GetCommand : GitCommandBase
    {
        public GetCommand(ICommandContext context, IHostProviderRegistry hostProviderRegistry)
            : base(context, "get", "[Git] Return a stored credential", hostProviderRegistry) { }

        protected override async Task ExecuteInternalAsync(InputArguments input, IHostProvider provider)
        {
            GetCredentialResult result = await provider.GetCredentialAsync(input);

            var output = new Dictionary<string, string>();

            // Echo protocol, host, and path back at Git
            if (input.Protocol != null)
            {
                output["protocol"] = input.Protocol;
            }
            if (input.Host != null)
            {
                output["host"] = input.Host;
            }
            if (input.Path != null)
            {
                output["path"] = input.Path;
            }

            // Return the credential to Git
            output["username"] = result.Credential.Account;
            output["password"] = result.Credential.Password;

            // Return result/credential metadata
            if (!string.IsNullOrWhiteSpace(result.CredentialType))
            {
                output["type"] = result.CredentialType;
            }

            foreach (KeyValuePair<string, string> kvp in result.AdditionalProperties)
            {
                output[kvp.Key] = kvp.Value;
            }

            // Write the values to standard out
            Context.Streams.Out.WriteDictionary(output);
        }
    }
}
