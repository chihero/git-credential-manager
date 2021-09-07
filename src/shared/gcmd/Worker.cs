using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Git.CredentialManager.Daemon
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const string pipePath = "/Users/mattche/.gcm/.pipe";
            var server = new PipeServer(pipePath);
            return server.StartAsync(stoppingToken);
        }
    }
}
