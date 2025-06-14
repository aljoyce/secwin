using Microsoft.Extensions.Options;
using secwin_lib;
using System.Net;
using System.Text.Json;

namespace secwin_srv
{
    public class Worker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly SecwinAppSettings _settings;
        private readonly SocketServer _socketServer;
         
        public Worker(LoggerSingleton logger, IOptions<SecwinAppSettings> options, SocketServer socketServer)
        {
            _logger = logger.Logger;
            _settings = options.Value;
            _socketServer = socketServer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await SetupSocketServer();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping the service");
            _socketServer.StopServer();
            await base.StopAsync(cancellationToken);
        }

        private async Task PerformSearch(SocketServer socketServer, string ClientId, string Message)
        {
            _logger.LogInformation("EVENT: Received: {message}", Message);

            var searchResults = await SearchService.DoSearch(Message);

            var searchData = new
            {
                Message = searchResults,
                Timestamp = DateTime.UtcNow,
                Status = "connected",
                Type = "SEARCH"
            };

            _logger.LogInformation("Sending data to client: {searchData}", searchData);
            string message = JsonSerializer.Serialize(searchData);
            await socketServer.SendToClient(ClientId, message);
        }

        private async Task SetupSocketServer()
        {
            // Configure the server
            var socketServer = new SocketServer(_settings.CertificatePath, _settings.CertificatePassword);

            socketServer.ClientConnected += (clientId) => _logger.LogInformation("EVENT: Client {clientId} connected", clientId);
            socketServer.ClientDisconnected += (clientId) => _logger.LogInformation("EVENT: Client {clientId} disconnected", clientId);

            socketServer.MessageReceived += async (clientId, message) => await PerformSearch(socketServer, clientId, message);

            try
            {
                await socketServer.StartAsync(IPAddress.Any, _settings.ServicePort);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occurred");
            }
        }
    }
}
