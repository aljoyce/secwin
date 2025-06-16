using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace secwin.core
{
    public class DashboardAdapter : IDashboardAdapter
    {
        private readonly ILogger<DashboardAdapter> _logger;

        private readonly SecwinAppSettings _settings;

        /// <summary>
        /// The connection used by the adapter to communicate with the service.
        /// This connection is responsible for handling the underlying communication
        /// </summary>
        // private IConnection? _connection;
        private SecureSocketConnection? _connection;

        public DashboardAdapter(ILogger<DashboardAdapter> logger, IOptions<SecwinAppSettings> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ArgumentNullException.ThrowIfNull(options);
            _settings = options.Value;
        }

        private async Task OnMessageReceived(string clientId, string message)
        {
            _logger.LogInformation("Message received from client {clientId}: {message}", clientId, message);

            // TODO - handle different messages with command pattern
            var searchResults = await SearchService.DoSearch(message);

            var searchData = new
            {
                Message = searchResults,
                Timestamp = DateTime.UtcNow,
                Status = "connected",
                Type = "SEARCH"
            };

            _logger.LogInformation("Sending data to client: {searchData}", searchData);
            string responseMessage = JsonSerializer.Serialize(searchData);
            await (_connection as SecureSocketConnection).Send(clientId, responseMessage);
        }

        private void OnClientConnected(string clientId)
        {
            _logger.LogInformation("Client connected: {clientId}", clientId);
        }

        private void OnClientDisconnected(string clientId)
        {
            _logger.LogInformation("Client disconnected: {clientId}", clientId);
        }

        public async Task Connect()
        {
            var settings = new SecureSocketSettings
            {
                CertificatePath = _settings.CertificatePath,
                CertificatePassword = _settings.CertificatePassword,
                Port = _settings.ServicePort,
                Host = IPAddress.Any.ToString(),
                MaxMessageSize = 4096 // 4 KB
            };

            _connection = new SecureSocketConnection();

            _connection.Configure(settings);

            _connection.MessageReceived += OnMessageReceived;
            _connection.ClientConnected += OnClientConnected;
            _connection.ClientDisconnected += OnClientDisconnected;

            await _connection.Connect();
        }

        public void Disconnect()
        {
            _logger.LogInformation("Disconnecting from the service.");
            _connection?.Disconnect();
        }
    }
}