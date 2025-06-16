using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace secwin.core
{
    public class SecureSocketConnection : IConnection
    {
        private readonly ILogger _logger = LoggerSingleton.Instance.Logger;

        /// <summary>
        /// Bare socket for communication and SSL stream for secure communication.
        /// </summary>
        private Socket? _socket;

        /// <summary>
        /// The settings for the connection method.
        /// </summary>
        private SecureSocketSettings? _settings;

        /// <summary>
        /// Certificate used for SSL/TLS communication.
        /// </summary>
        private X509Certificate2? _certificate;

        /// <summary>
        /// The endpoint to which the socket is connected.
        /// </summary>
        private IPEndPoint? _endPoint;

        /// <summary>
        /// Dictionary of client bare socket connections
        /// </summary>
        private readonly ConcurrentDictionary<string, Socket> _clients;

        /// <summary>
        /// Dictionary of client SSL stream connections
        /// </summary>
        private readonly ConcurrentDictionary<string, SslStream> _sslClients;

        /// <summary>
        /// Cancellation token source for managing cancellation of operations.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Flag indicating whether the socket is running.
        /// </summary>
        private bool _isRunning = false;

        public event Func<string, string, Task>? MessageReceived;

        public event Action<string>? ClientConnected;

        public event Action<string>? ClientDisconnected;

        public SecureSocketConnection()
        {
            _clients = new ConcurrentDictionary<string, Socket>();
            _sslClients = new ConcurrentDictionary<string, SslStream>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Configure(ConnectionSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "Connection settings cannot be null.");

            if (settings is not SecureSocketSettings secureSettings)
                throw new ArgumentException("SecureSocketConnector requires SecureSocketSettings.", nameof(settings));

            if (secureSettings.Port <= 0 || secureSettings.Port > 65535)
                throw new ArgumentOutOfRangeException(nameof(settings), "Port must be between 1 and 65535.");

            _settings = secureSettings;

            _endPoint = new IPEndPoint(IPAddress.Parse(_settings.Host), _settings.Port);

            _certificate = X509CertificateLoader.LoadPkcs12FromFile(
                _settings.CertificatePath,
                _settings.CertificatePassword,
                keyStorageFlags: X509KeyStorageFlags.MachineKeySet
            );
        }

        public Task Connect()
        {
            if (_endPoint == null)
                throw new InvalidOperationException("EndPoint is not configured. Call Configure() before Connect().");

            _logger.LogInformation("Starting server on {ipAddress}:{port}", _endPoint.Address, _endPoint.Port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _socket.Bind(_endPoint);
            _socket.Listen(10);

            _isRunning = true;

            // make sure this runs before console write line
            _logger.LogInformation("Successful server startup on {ipAddress}:{port}", _endPoint?.Address, _endPoint?.Port);

            Task.Run(async() =>
            {
                // TODO - Move you to a background task
                while (_isRunning && !_cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var client = await AcceptClientAsync();
                        if (client != null)
                        {
                            string clientId = Guid.NewGuid().ToString();
                            _clients.TryAdd(clientId, client);

                            ClientConnected?.Invoke(clientId);
                            _logger.LogInformation("Client {clientId} connected from {client.RemoteEndPoint}", clientId, client.RemoteEndPoint);

                            _ = Task.Run(async () => await HandleClientAsync(clientId, client));
                        }
                    }
                    catch (ObjectDisposedException ex)
                    {
                        _logger.LogWarning(ex, "Object was disposed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error accepting client connection");
                    }
                }
            });

            return Task.CompletedTask;
        }

        public bool Status() => _isRunning;

        public void Disconnect()
        {
            _logger.LogInformation("Stopping server");

            if (_isRunning)
            {
                _cancellationTokenSource.Cancel();

                // Snapshot of current clients
                var clientIds = _clients.Keys.ToList();

                // Looping through the bare clients should include all the SSL clients as well
                foreach (var clientId in clientIds)
                {
                    try
                    {
                        _clients.TryRemove(clientId, out var client);
                        _sslClients.TryRemove(clientId, out var sslClient);

                        _logger.LogInformation("Disconnecting client {clientId} at {client.RemoteEndPoint}", clientId, client?.RemoteEndPoint);

                        sslClient?.Close();
                        client?.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to close client connection for client {clientId}", clientId);
                    }
                }

                try
                {
                    _socket?.Close();
                    _isRunning = false;
                    _logger.LogInformation("Server stopped");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to close server connection");
                }
            }
        }

        public async Task Send(string clientId, string message)
        {
            if (!_isRunning)
                throw new InvalidOperationException("Cannot send message, not connected.");

            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException(nameof(clientId), "Client ID cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message), "Message cannot be null or empty.");

            if (message.Length > _settings?.MaxMessageSize)
                throw new ArgumentException($"Message exceeds maximum size of {_settings?.MaxMessageSize} bytes.");

            if (!_sslClients.TryGetValue(clientId, out var sslStream))
                throw new InvalidOperationException($"Client {clientId} is not connected.");

            if (sslStream == null)
                throw new InvalidOperationException($"SSL stream for client {clientId} is not initialized.");

            _logger.LogDebug("Sending message: {message}", message);

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            await sslStream.WriteAsync(messageBytes);
            await sslStream.FlushAsync();
        }

        private async Task SendWelcomeMessage(string clientId)
        {
            var welcomeMessage = new
            {
                Message = "You are connected to the secwin service",
                Timestamp = DateTime.UtcNow,
                Status = "connected",
                Type = "CONNECTION"
            };

            await Send(clientId, JsonSerializer.Serialize(welcomeMessage));
        }

        private async Task<Socket?> AcceptClientAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_socket == null)
                {
                    _logger.LogError("Socket is not initialized.");
                    return null;
                }
                return await _socket.AcceptAsync(cancellationToken);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "Object was disposed");
                return null;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                _logger.LogWarning(ex, "Accept was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred accepting the client");
                return null;
            }
        }

        private async Task HandleClientAsync(string clientId, Socket client)
        {
            // To get TLS on a bare socket, we wrap it in a network stream and then  wrap the network stream in an ssl stream
            var netstream = new NetworkStream(client, ownsSocket: false);
            var sslStream = new SslStream(netstream, leaveInnerStreamOpen: false);

            if (_settings == null)
                throw new InvalidOperationException("Settings must be configured before handling clients.");

            var buffer = new byte[_settings.MaxMessageSize];

            try
            {
                if (_certificate == null)
                    throw new InvalidOperationException("Certificate must be loaded before authenticating as server.");

                // We have to do auth first
                await sslStream.AuthenticateAsServerAsync(
                    _certificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12,
                    checkCertificateRevocation: false
                );

                _sslClients.TryAdd(clientId, sslStream);

                await SendWelcomeMessage(clientId);

                while (_isRunning && client.Connected)
                {
                    try
                    {
                        int bytesReceived = await sslStream.ReadAsync(buffer);

                        if (bytesReceived == 0)
                        {
                            _logger.LogInformation("Nothing recieved from client {clientId}", clientId);
                            break;
                        }

                        string data = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        _logger.LogDebug("Client: {clientId} - Received: {data}", clientId, data);

                        MessageReceived?.Invoke(clientId, data);
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogError(ex, "Error with socket for client {clientId}", clientId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client {clientId}", clientId);
            }
            finally
            {
                // If we got an error handling the client, we will just remove it
                _sslClients.TryRemove(clientId, out _);
                _clients.TryRemove(clientId, out _);
                sslStream?.Close();
                client?.Close();
                ClientDisconnected?.Invoke(clientId);
                _logger.LogInformation("Ended the handler for client {clientId}", clientId);
            }
        }
    }
}