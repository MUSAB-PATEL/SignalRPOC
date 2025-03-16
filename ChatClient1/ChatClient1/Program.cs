using Microsoft.AspNetCore.SignalR.Client;
using System.Security.Cryptography.X509Certificates;

class Program
{
    static async Task Main(string[] args)
    {
        string userKey = "client1-key";
        string username = "Client1";
        string role = "Client";
        string assignedRM = "rm1-key";

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
            await connection.InvokeAsync("RegisterUser", userKey, username, role, new List<string> { assignedRM });
        };

        try
        {
            await connection.StartAsync();
            Console.WriteLine("Connected to SignalR Hub.");

            await connection.InvokeAsync("RegisterUser", userKey, username, role, new List<string> { assignedRM });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting: {ex.Message}");
            return;
        }

        while (true)
        {
            Console.Write("\nEnter message to RM: ");
            string message = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(message)) continue;

            try
            {
                await connection.InvokeAsync("SendMessageToGroup", userKey, assignedRM, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }
    }
}