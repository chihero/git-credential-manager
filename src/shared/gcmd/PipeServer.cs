using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Daemon
{
    public class PipeServer : IDisposable
    {
        private readonly string _pipeName;
        private readonly ConcurrentDictionary<Guid, ClientHandler> _clients;
        private readonly CancellationTokenSource _cts;

        private bool _isStarted;
        private bool _isDisposed;

        public PipeServer(string pipeName)
        {
            _pipeName = pipeName;
            _clients = new ConcurrentDictionary<Guid, ClientHandler>();
            _cts = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken ct)
        {
            ThrowIfDisposed();

            if (_isStarted)
            {
                throw new InvalidOperationException("Already started");
            }

            _isStarted = true;

            ct.Register(Stop);

            return Task.Run(() => ListenerThreadAsync(_cts.Token), _cts.Token);
        }

        private void Stop()
        {
            ThrowIfDisposed();

            if (!_isStarted)
            {
                return;
            }

            _cts.Cancel();

            // Stop all client handlers
            Guid[] ids = _clients.Keys.ToArray();
            foreach (Guid id in ids)
            {
                if (_clients.TryRemove(id, out ClientHandler handler))
                {
                    handler.Dispose();
                }
            }

            _isStarted = false;
        }

        private async void ListenerThreadAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    ClientHandler handler = await AcceptAsync(ct);
                    if (_clients.TryAdd(handler.Id, handler))
                    {
                        handler.Start();
                    }
                    else
                    {
                        handler.Dispose();
                    }
                }
                catch (OperationCanceledException) { }
            }
        }

        private async Task<ClientHandler> AcceptAsync(CancellationToken ct)
        {
            var pipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, -1);
            try
            {
                await pipe.WaitForConnectionAsync(ct);
                return new ClientHandler(pipe);
            }
            catch (OperationCanceledException)
            {
                await pipe.DisposeAsync();
                throw;
            }
        }


        #region IDisposable

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PipeServer));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Stop();
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
}
