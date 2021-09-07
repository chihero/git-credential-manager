using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket;
using GitHub;
using Microsoft.AzureRepos;

namespace Microsoft.Git.CredentialManager.Daemon
{
    internal class ClientHandler : IDisposable
    {
        private readonly Guid _id;
        private readonly NamedPipeServerStream _pipe;
        private readonly CancellationTokenSource _cts;

        private bool _isStarted;
        private bool _isDisposed;

        public ClientHandler(NamedPipeServerStream pipe)
        {
            _id = Guid.NewGuid();
            _pipe = pipe;
            _cts = new CancellationTokenSource();
        }

        public Guid Id => _id;

        public void Start()
        {
            ThrowIfDisposed();

            if (_isStarted)
            {
                throw new InvalidOperationException("Already started");
            }

            _isStarted = true;

            Task.Run(() => SenderThreadAsync(_cts.Token), _cts.Token);
        }

        public void Stop()
        {
            ThrowIfDisposed();

            if (!_isStarted)
            {
                return;
            }

            _cts.Cancel();

            _isStarted = false;
        }

        private async void SenderThreadAsync(CancellationToken ct)
        {
            try
            {
                var reader = new StreamReader(_pipe);
                string command = await reader.ReadLineAsync();

                var pipeStreams = new DaemonPipeStreams(_pipe);

                string appPath = ApplicationBase.GetEntryApplicationPath();
                using (var context = new CommandContext(appPath, pipeStreams))
                using (var app = new Application(context))
                {
                    // Register all supported host providers at the normal priority.
                    // The generic provider should never win against a more specific one, so register it with low priority.
                    app.RegisterProvider(new AzureReposHostProvider(context), HostProviderPriority.Normal);
                    app.RegisterProvider(new BitbucketHostProvider(context), HostProviderPriority.Normal);
                    app.RegisterProvider(new GitHubHostProvider(context), HostProviderPriority.Normal);
                    app.RegisterProvider(new GenericHostProvider(context), HostProviderPriority.Low);

                    int exitCode = app.RunAsync(new[] { command })
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();

                    _pipe.Close();
                }

            }
            catch (OperationCanceledException) { }
            catch (IOException)
            {
                if (!_pipe.IsConnected)
                {
                    // Broken pipe! Client disconnected without warning
                }
            }
        }

        #region IDisposable

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ClientHandler));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Stop();
                    _pipe.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }

    internal class DaemonPipeStreams : IStandardStreams
    {
        private const string LineFeed  = "\n";

        private static readonly Encoding Utf8NoBomEncoding = new UTF8Encoding(false);

        private readonly NamedPipeServerStream _pipe;
        private TextReader _stdIn;
        private TextWriter _stdOut;
        private TextWriter _stdErr;

        public DaemonPipeStreams(NamedPipeServerStream pipe)
        {
            _pipe = pipe;
        }

        public TextReader In => _stdIn ??= new StreamReader(_pipe, Utf8NoBomEncoding);

        public TextWriter Out => _stdOut ??= new StreamWriter(_pipe, Utf8NoBomEncoding)
        {
            AutoFlush = true,
            NewLine = LineFeed,
        };

        public TextWriter Error => _stdErr ??= new StreamWriter(Stream.Null);
    }
}
