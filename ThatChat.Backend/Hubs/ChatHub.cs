using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ThatChat.Backend.Data;

namespace ThatChat.Backend.Hubs;

public record ChatMessageDto(
	[UsedImplicitly] Guid Id,
	Guid ChatId,
	Guid SenderId,
	string? SenderEmail,
	string? Text,
	string? FileUrl,
	DateTime CreatedAt
);

public interface IChatClient
{
	Task ReceiveChatMessage(ChatMessageDto message);
}

[Authorize]
public class ChatHub : Hub<IChatClient>
{
	private readonly ILogger<ChatHub> _logger;
	private readonly AppDbContext _db;

	public ChatHub(ILogger<ChatHub> logger, AppDbContext db)
	{
		_logger = logger;
		_db = db;
	}

	public async Task JoinChat(Guid chatId)
	{
		var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
		{
			return;
		}

		var hasAccess = await _db.Chats
			.AnyAsync(c => c.Id == chatId && (c.IsGlobal || c.ChatUsers.Any(cu => cu.UserId == userId)));

		if (!hasAccess)
		{
			_logger.LogWarning("User {UserId} tried to join chat {ChatId} without access.", userId, chatId);
			return;
		}

		await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
		_logger.LogInformation("User {UserId} joined group {ChatId}", userId, chatId);
	}

	public async Task LeaveChat(Guid chatId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
	}

	public async Task SendMessage(Guid chatId, string message, string? fileUrl)
	{
		if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(fileUrl))
		{
			return;
		}

		if (message.Length > 10_000)
		{
			return;
		}

		var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
		{
			return;
		}

		var chat = await _db.Chats
			.Where(c => c.Id == chatId && (c.IsGlobal || c.ChatUsers.Any(cu => cu.UserId == userId)))
			.FirstOrDefaultAsync();

		if (chat == null)
		{
			return;
		}

		var msgEnt = new MessageEnt
		{
			Id = Guid.NewGuid(),
			ChatId = chatId,
			SenderId = userId,
			Text = message,
			FileUrl = fileUrl,
			CreatedAt = DateTime.UtcNow,
		};

		_db.Messages.Add(msgEnt);
		await _db.SaveChangesAsync();

		var senderEmail = await _db.Users
			.Where(u => u.Id == userId)
			.Select(u => u.Email)
			.FirstOrDefaultAsync();

		var dto = new ChatMessageDto(
			msgEnt.Id,
			msgEnt.ChatId,
			msgEnt.SenderId,
			senderEmail,
			msgEnt.Text,
			msgEnt.FileUrl,
			msgEnt.CreatedAt
		);

		await Clients.Group(chatId.ToString()).ReceiveChatMessage(dto);
	}

	public override Task OnConnectedAsync()
	{
		_logger.LogInformation("Client connected: {connectionId}", Context.ConnectionId);
		return base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		_logger.LogInformation("Client disconnected: {connectionId}", Context.ConnectionId);
		return base.OnDisconnectedAsync(exception);
	}
}
