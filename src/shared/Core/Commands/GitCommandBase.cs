using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace GitCredentialManager.Commands
{
    /// <summary>
    /// Represents a command which selects a <see cref="IHostProvider"/> from a <see cref="IHostProviderRegistry"/>
    /// based on the <see cref="InputArguments"/> from standard input, and interacts with a <see cref="GitCredential"/>.
    /// </summary>
    public abstract class GitCommandBase : Command
    {
        private readonly IHostProviderRegistry _hostProviderRegistry;

        protected GitCommandBase(ICommandContext context, string name, string description, IHostProviderRegistry hostProviderRegistry)
            : base(name, description)
        {
            EnsureArgument.NotNull(hostProviderRegistry, nameof(hostProviderRegistry));
            EnsureArgument.NotNull(context, nameof(context));

            Context = context;
            _hostProviderRegistry = hostProviderRegistry;

            Handler = CommandHandler.Create(ExecuteAsync);
        }

        protected ICommandContext Context { get; }

        internal async Task ExecuteAsync()
        {
            Context.Trace.WriteLine($"Start '{Name}' command...");

            // Parse standard input arguments
            // git-credential treats the keys as case-sensitive; so should we.
            IDictionary<string, string> inputDict = await Context.Streams.In.ReadDictionaryAsync(StringComparer.Ordinal);

            // Newer Git clients will include additional response headers after the input
            // arguments block (hidden behind the terminating newline).
            // Attempt to read the standard input stream until the next terminating newline!
            IList<string> headers = await Context.Streams.In.ReadListAsync();

            var input = new InputArguments(inputDict, headers);

            // Validate minimum arguments are present
            EnsureMinimumInputArguments(input);

            // Set the remote URI to scope settings to throughout the process from now on
            Context.Settings.RemoteUri = input.GetRemoteUri();

            // Determine the host provider
            Context.Trace.WriteLine("Detecting host provider for input:");
            Context.Trace.WriteDictionarySecrets(inputDict, new []{ "password" }, StringComparer.OrdinalIgnoreCase);
            if (headers.Count > 0)
            {
                Context.Trace.WriteLine("Headers:");
                foreach (string header in headers)
                {
                    Context.Trace.WriteLine($"\t{header}");
                }
            }

            IHostProvider provider = await _hostProviderRegistry.GetProviderAsync(input);
            Context.Trace.WriteLine($"Host provider '{provider.Name}' was selected.");

            // Run the requested command!
            await ExecuteInternalAsync(input, provider);

            Context.Trace.WriteLine($"End '{Name}' command...");
        }

        protected virtual void EnsureMinimumInputArguments(InputArguments input)
        {
            if (input.Protocol is null)
            {
                throw new InvalidOperationException("Missing 'protocol' input argument");
            }

            if (string.IsNullOrWhiteSpace(input.Protocol))
            {
                throw new InvalidOperationException("Invalid 'protocol' input argument (cannot be empty)");
            }

            if (input.Host is null)
            {
                throw new InvalidOperationException("Missing 'host' input argument");
            }

            if (string.IsNullOrWhiteSpace(input.Host))
            {
                throw new InvalidOperationException("Invalid 'host' input argument (cannot be empty)");
            }
        }

        /// <summary>
        /// Execute the command using the given <see cref="InputArguments"/> and <see cref="IHostProvider"/>.
        /// </summary>
        /// <param name="input">Input arguments of the current Git credential query.</param>
        /// <param name="provider">Host provider for the current <see cref="InputArguments"/>.</param>
        /// <returns>Awaitable task for the command execution.</returns>
        protected abstract Task ExecuteInternalAsync(InputArguments input, IHostProvider provider);
    }
}
