using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Identity.Client;

namespace Microsoft.Authentication
{
    public interface IMicrosoftPrompts
    {
        Task ShowDeviceCodeAsync(DeviceCodeResult deviceCodeResult);
    }

    public class MicrosoftTerminalPrompts : TerminalPrompts, IMicrosoftPrompts
    {
        public MicrosoftTerminalPrompts(ICommandContext context)
            : base(context.Settings, context.Terminal) { }

        public MicrosoftTerminalPrompts(ITrace trace, ISettings settings, ITerminal terminal)
            : base(settings, terminal) { }

        public Task ShowDeviceCodeAsync(DeviceCodeResult deviceCodeResult)
        {
            Terminal.WriteLine(deviceCodeResult.Message);
            return Task.CompletedTask;
        }
    }
}
