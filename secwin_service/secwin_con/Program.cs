using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using secwin_lib;

namespace secwin_con
{
    /// <summary>
    /// This console application is used for testing
    /// </summary>
    public class Program
    {
        private static readonly ILogger Log = LoggerSingleton.Instance.Logger;

        public static async Task Main(string[] args)
        {
            Log.LogInformation("Test program running");

            await SetupSocketServer();
        }

        public static async Task PerformSearch(SocketServer socketServer, string ClientId, string Message)
        {
            Log.LogInformation("EVENT: Received: {message}", Message);

            var searchResults = await SearchService.DoSearch(Message);

            var searchData = new
            {
                Message = searchResults,
                Timestamp = DateTime.UtcNow,
                Status = "connected",
                Type = "SEARCH"
            };

            Log.LogInformation("Sending data to client: {searchData}", searchData);
            string message = JsonSerializer.Serialize(searchData);
            await socketServer.SendToClient(ClientId, message);
        }

        public static async Task SetupSocketServer()
        {
            string certificatePath = "./certs/secwin.pfx";
            string certificatePassword = "secwin123";

            // Configure the server
            var socketServer = new SocketServer(certificatePath, certificatePassword);

            socketServer.ClientConnected += (clientId) => Log.LogInformation("EVENT: Client {clientId} connected", clientId);
            socketServer.ClientDisconnected += (clientId) => Log.LogInformation("EVENT: Client {clientId} disconnected", clientId);

            socketServer.MessageReceived += async (clientId, message) => await PerformSearch(socketServer, clientId, message);

            Console.CancelKeyPress += (sender, e) =>
            {
                Log.LogInformation("Operation was stopped with Control+C");
                e.Cancel = true;
                socketServer.StopServer();
            };

            try
            {
                await socketServer.StartAsync(IPAddress.Any, 42042);
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "An error has occurred");
            }
        }
    }
}