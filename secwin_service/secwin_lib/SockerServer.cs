using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace secwin_lib
{
    public class SocketServer
    {
        private readonly ILogger Log = LoggerSingleton.Instance.Logger;

        // Socket related variables
        private readonly Socket _serverSocket;
        private readonly ConcurrentDictionary<string, Socket> _clients;
        private readonly ConcurrentDictionary<string, SslStream> _sslClients;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;

        // Actions to handle the socket events
        public event Action<string, string>? MessageReceived;
        public event Action<string>? ClientConnected;
        public event Action<string>? ClientDisconnected;

        private const int MAX_MESSAGE_SIZE = 4096;

        private static X509Certificate2 _serverCertificate;

        public SocketServer(string certificatePath, string certificatePassword)
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clients = new ConcurrentDictionary<string, Socket>();
            _sslClients = new ConcurrentDictionary<string, SslStream>();
            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = false;

            _serverCertificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, certificatePassword, keyStorageFlags: X509KeyStorageFlags.MachineKeySet);
        }

        public async Task StartAsync(IPAddress ipAddress, int port)
        {
            try
            {
                Log.LogInformation("Starting server on {ipAddress}:{port}", ipAddress, port);

                _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _serverSocket.Bind(new IPEndPoint(ipAddress, port));
                _serverSocket.Listen(10);
                _isRunning = true;

                Log.LogInformation("Successful server startup on {ipAddress}:{port}", ipAddress, port);

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
                            Log.LogInformation("Client {clientId} connected from {client.RemoteEndPoint}", clientId, client.RemoteEndPoint);

                            _ = Task.Run(async () => await HandleClientAsync(clientId, client));
                        }
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Log.LogWarning(ex, "Object was disposed");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(ex, "Error accepting client connection");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogCritical(ex, "Server startup failed:");
            }
        }

        public void StopServer()
        {
            Log.LogInformation("Stopping server");

            if (_isRunning)
            {
                _cancellationTokenSource.Cancel();

                if (_clients.IsEmpty)
                    Log.LogInformation("No clients have connected");
                else
                {
                    foreach (var client in _clients)
                    {
                        try
                        {
                            Log.LogInformation("Disconnecting client {clientId} at {client.RemoteEndPoint}", client.Key, client.Value.RemoteEndPoint);
                            _sslClients.TryRemove(client.Key, out var sslClient);
                            sslClient?.Close();
                            client.Value.Close();
                        }
                        catch (Exception ex)
                        {
                            Log.LogError(ex, "Failed to close client connection");
                        }
                    }
                    _clients.Clear();
                    _sslClients.Clear();
                }

                try
                {
                    _serverSocket.Close();
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, "Failed to close server connection");
                }
                _isRunning = false;
            }
            Log.LogInformation("Server stopped");
        }

        public async Task SendToClient(string clientId, string message)
        {
            if (!_sslClients.TryGetValue(clientId, out var client))
            {
                Log.LogError("The clientId could not be found {clientId}", clientId);
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(message);

            try
            {
                await client.WriteAsync(data);
                await client.FlushAsync();
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Failed to send the message to the client {clientId}", clientId);
            }
        }

        private async Task<Socket?> AcceptClientAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _serverSocket.AcceptAsync(cancellationToken);
            }
            catch (ObjectDisposedException ex)
            {
                Log.LogWarning(ex, "Object was disposed");
                return null;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                Log.LogWarning(ex, "Accept was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "An error occurred accepting the client");
                return null;
            }
        }

        private async Task HandleClientAsync(string clientId, Socket client)
        {
            // To get TLS on a bare socket, we wrap it in a network stream and then
            // wrap the network stream in an ssl stream
            var netstream = new NetworkStream(client, ownsSocket: false);
            var sslStream = new SslStream(netstream, leaveInnerStreamOpen: false);

            var buffer = new byte[MAX_MESSAGE_SIZE];

            try
            {
                // We have to do auth first
                await sslStream.AuthenticateAsServerAsync(
                    _serverCertificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12,
                    checkCertificateRevocation: false
                );

                _sslClients.TryAdd(clientId, sslStream);

                var welcomeData = new
                {
                    Message = "You are connected to the secwin service",
                    Timestamp = DateTime.UtcNow,
                    Status = "connected",
                    Type = "CONNECTION"
                };
                string welcomeJsonString = JsonSerializer.Serialize(welcomeData);
                byte[] welcome_message = Encoding.UTF8.GetBytes(welcomeJsonString);

                await sslStream.WriteAsync(welcome_message);
                await sslStream.FlushAsync();

                while (_isRunning && client.Connected)
                {
                    try
                    {
                        int bytesReceived = await sslStream.ReadAsync(buffer);

                        if (bytesReceived == 0)
                        {
                            Log.LogInformation("Nothing recieved from client {clientId}", clientId);
                            break;
                        }

                        string data = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        Log.LogDebug("Client: {clientId} - Received: {data}", clientId, data);

                        MessageReceived?.Invoke(clientId, data);
                    }
                    catch (SocketException ex)
                    {
                        Log.LogError(ex, "Error with socket for client {clientId}", clientId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Error handling client {clientId}", clientId);
            }
            finally
            {
                // If we got an error handling the client, we will just remove it
                _sslClients.TryRemove(clientId, out _);
                _clients.TryRemove(clientId, out _);
                sslStream?.Close();
                client?.Close();
                ClientDisconnected?.Invoke(clientId);
                Log.LogInformation("Ended the handler for client {clientId}", clientId);
            }
        }
    }
}