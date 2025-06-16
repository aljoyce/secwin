namespace secwin.core
{
    public interface IConnection
    {
        /// <summary>
        /// Event triggered when a message is received from a client.
        /// The event provides the client ID and the message content.
        /// </summary>
        public event Func<string, string, Task>? MessageReceived;

        /// <summary>
        /// Event triggered when a client connects to the service.
        /// The event provides the client ID of the connected client.
        /// </summary>
        public event Action<string>? ClientConnected;

        /// <summary>
        /// Event triggered when a client disconnects from the service.
        /// The event provides the client ID of the disconnected client.
        /// </summary>
        public event Action<string>? ClientDisconnected;

        /// <summary>
        /// Configures the connection settings for the service.
        /// </summary>
        /// <param name="settings">The connection settings for the connection method</param>
        void Configure(ConnectionSettings settings);

        /// <summary>
        /// Connects to the service.
        /// </summary>
        Task Connect();

        /// <summary>
        /// Checks the connection status.
        /// </summary>
        bool Status();

        /// <summary>
        /// Disconnects from the service.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sends a message to the service.
        /// </summary>
        /// <param name="clientId">The ID of the client to send the message to.</param>
        /// <param name="message">The message to send.</param>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the clientId or message is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when the message exceeds the maximum allowed size.</exception>
        Task Send(string clientId, string message);
    }
}