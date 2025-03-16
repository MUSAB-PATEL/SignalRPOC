using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SignalR_POC.Controllers.ChatHubs;

namespace SignalR_POC.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromQuery] string senderKey, [FromQuery] string receiverKey, [FromQuery] string message)
        {
            senderKey = senderKey.Trim().ToLower();
            receiverKey = receiverKey.Trim().ToLower();

            if (!ChatHub._users.TryGetValue(senderKey, out var senderData))
            {
                return NotFound(new { Error = "Sender is not connected." });
            }

            if (!ChatHub._users.TryGetValue(receiverKey, out var receiverData))
            {
                return NotFound(new { Error = "Receiver is not connected." });
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
                return BadRequest(new { Error = "Invalid sender/receiver roles for messaging." });
            }

            string groupName = $"{clientKey}_{rmKey}";

            await _hubContext.Clients.GroupExcept(groupName, senderData.ConnectionIds)
                .SendAsync("ReceiveMessage", senderData.Username, message);

            return Ok(new { Message = "Message sent successfully." });
        }
    }
}
