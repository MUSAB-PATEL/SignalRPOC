using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SignalR_POC.Controllers.ChatHubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        internal static ConcurrentDictionary<string, UserInfo> _users = new();

        public async Task RegisterUser(string userKey, string username, string role, List<string> assignedClients = null)
        {
            userKey = userKey.Trim().ToLower(); 
            username = username.Trim();
            role = role.Trim().ToLower();

            if (role == "client" && (assignedClients == null || assignedClients.Count == 0))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Clients must have an assigned RM.");
                return;
            }

            if (_users.TryGetValue(userKey, out var existingUser))
            {
                existingUser.ConnectionIds.Add(Context.ConnectionId);
            }
            else
            {
                var user = new UserInfo
                {
                    UserKey = userKey,
                    Username = username,
                    Role = role,
                    AssignedClients = assignedClients?.Select(c => c.ToLower()).ToList() ?? new List<string>()
                };
                user.ConnectionIds.Add(Context.ConnectionId);
                _users[userKey] = user;
            }

            if (role == "client")
            {
                string rmUserKey = assignedClients.First().ToLower();
                string groupName = $"{userKey}_{rmUserKey}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await Clients.Caller.SendAsync("ReceiveMessage", "System", $"User {username} registered and joined {groupName}.");
            }
            else if (role == "rm")
            {
                foreach (var clientKey in assignedClients)
                {
                    string groupName = $"{clientKey.ToLower()}_{userKey}";
                    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                }
                await Clients.Caller.SendAsync("ReceiveMessage", "System", $"User {username} registered and joined multiple groups.");
            }
        }

        public async Task SendMessageToGroup(string senderKey, string receiverKey, string message)
        {
            senderKey = senderKey.Trim().ToLower();
            receiverKey = receiverKey.Trim().ToLower();

            if (!_users.TryGetValue(senderKey, out var senderData))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Sender is not registered.");
                return;
            }

            if (!_users.TryGetValue(receiverKey, out var receiverData))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Receiver is not registered.");
                return;
            }

            string clientKey, rmKey;
            if (senderData.Role == "client" && receiverData.Role == "rm")
            {
                clientKey = senderKey;
                rmKey = receiverKey;
            }
            else if (senderData.Role == "rm" && receiverData.Role == "client")
            {
                clientKey = receiverKey;
                rmKey = senderKey;
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Invalid sender/receiver roles for messaging.");
                return;
            }

            string groupName = $"{clientKey}_{rmKey}";
            await Clients.OthersInGroup(groupName).SendAsync("ReceiveMessage", senderData.Username, message);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userEntry = _users.FirstOrDefault(u => u.Value.ConnectionIds.Contains(Context.ConnectionId));

            if (!string.IsNullOrEmpty(userEntry.Key))
            {
                var user = userEntry.Value;
                user.ConnectionIds.Remove(Context.ConnectionId);

                if (user.ConnectionIds.Count == 0)
                {
                    _users.TryRemove(userEntry.Key, out _);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    public class UserInfo
    {
        public string UserKey { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public List<string> AssignedClients { get; set; } = new();
        public HashSet<string> ConnectionIds { get; set; } = new();
    }
}

