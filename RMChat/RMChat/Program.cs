using Microsoft.AspNetCore.SignalR.Client;
using System.Security.Cryptography.X509Certificates;

class Program
{
    static async Task Main(string[] args)
    {
        string userKey = "RM1-Key";
        string username = "RM1";
        string role = "RM";
        List<string> assignedClients = new List<string> { "client1-key" };

        string serverUrl = "https://localhost:7299/chatHub";

        var certificate = new X509Certificate2("D:\\ssClientCert\\ssClientCert.pfx", "Client123");

        var httpHandler = new HttpClientHandler();
        httpHandler.ClientCertificates.Add(certificate);
        httpHandler.CheckCertificateRevocationList = false;

        var connection = new HubConnectionBuilder()
            .WithUrl(serverUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ =>
                {
                    var handler = new HttpClientHandler();
                    handler.ClientCertificates.Add(certificate);
                    handler.CheckCertificateRevocationList = false;
                    return handler;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            Console.WriteLine($"\n{user}: {message}");
        });

        connection.Reconnected += async _ =>
        {
            Console.WriteLine("Reconnected! Registering user again...");
            await connection.InvokeAsync("RegisterUser", userKey, username, role, assignedClients);
        };

        try
        {
            await connection.StartAsync();
            Console.WriteLine("Connected to SignalR Hub.");
            await connection.InvokeAsync("RegisterUser", userKey, username, role, assignedClients);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting: {ex.Message}");
            return;
        }

        while (true)
        {
            Console.Write("\nEnter Client's UserKey: ");
            string receiverKey = Console.ReadLine();
            Console.Write("Enter message: ");
            string message = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(receiverKey) || string.IsNullOrWhiteSpace(message)) continue;

            try
            {
                await connection.InvokeAsync("SendMessageToGroup", userKey, receiverKey, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }
    }
}