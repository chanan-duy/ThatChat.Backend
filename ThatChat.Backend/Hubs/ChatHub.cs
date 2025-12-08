using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ThatChat.Backend.Data;

namespace ThatChat.Backend.Hubs;

[Authorize]
public class ChatHub : Hub
{
	private readonly AppDbContext _db;

	public ChatHub(AppDbContext db)
	{
		_db = db;
	}

	public async Task JoinChat(Guid chatId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
	}

	public async Task SendMessage(Guid chatId, string messageText, string? fileUrl)
	{
		var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var msg = new MessageEnt
		{
			Id = Guid.NewGuid(),
			ChatId = chatId,
			SenderId = userId,
			Text = messageText,
			FileUrl = fileUrl,
			CreatedAt = DateTime.UtcNow,
		};

		_db.Messages.Add(msg);
		await _db.SaveChangesAsync();

		var senderEmail = await _db.Users
			.Where(u => u.Id == userId)
			.Select(u => u.Email)
			.FirstOrDefaultAsync();

		await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", new
		{
			msg.Id,
			msg.ChatId,
			msg.SenderId,
			SenderEmail = senderEmail,
			msg.Text,
			msg.FileUrl,
			msg.CreatedAt,
		});
	}
}
