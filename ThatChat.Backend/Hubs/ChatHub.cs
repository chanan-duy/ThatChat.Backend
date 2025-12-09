using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ThatChat.Backend.Dto;
using ThatChat.Backend.Services;

namespace ThatChat.Backend.Hubs;

public interface IChatClient
{
	Task ReceiveChatMessage(ChatMessageDto message);
}

[Authorize]
public class ChatHub(IChatService chatService) : Hub<IChatClient>
{
	public async Task JoinChat(Guid chatId)
	{
		var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
		{
			return;
		}

		if (await chatService.HasAccessToChatAsync(userId, chatId))
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
		}
	}

	public async Task LeaveChat(Guid chatId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
	}

	public async Task SendMessage(Guid chatId, string message, string? fileUrl)
	{
		var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
		{
			return;
		}

		var dto = await chatService.SaveMessageAsync(userId, chatId, message, fileUrl);

		if (dto != null)
		{
			await Clients.Group(chatId.ToString()).ReceiveChatMessage(dto);
		}
	}
}
