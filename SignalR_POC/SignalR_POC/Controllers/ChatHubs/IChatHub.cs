namespace SignalR_POC.Controllers.ChatHubs
{
    public interface IChatHub
    {
        Task ReceiveMessage(string user, string message);
    }
}
