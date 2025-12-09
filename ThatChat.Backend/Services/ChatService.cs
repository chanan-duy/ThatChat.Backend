using Microsoft.EntityFrameworkCore;
using ThatChat.Backend.Data;
using ThatChat.Backend.Dto;

namespace ThatChat.Backend.Services;

public class ChatService(AppDbContext db) : IChatService
{
	public async Task<bool> HasAccessToChatAsync(Guid userId, Guid chatId)
	{
		return await db.Chats
			.AnyAsync(c => c.Id == chatId && (c.IsGlobal || c.ChatUsers.Any(cu => cu.UserId == userId)));
	}

	public async Task<ChatMessageDto?> SaveMessageAsync(Guid userId, Guid chatId, string message, string? fileUrl)
	{
		if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(fileUrl))
		{
			return null;
		}

		if (message.Length > 10_000)
		{
			return null;
		}

		var hasAccess = await HasAccessToChatAsync(userId, chatId);
		if (!hasAccess)
		{
			return null;
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

		db.Messages.Add(msgEnt);
		await db.SaveChangesAsync();

		var senderEmail = await db.Users
			.Where(u => u.Id == userId)
			.Select(u => u.Email)
			.FirstOrDefaultAsync();

		return new ChatMessageDto(
			msgEnt.Id,
			msgEnt.ChatId,
			msgEnt.SenderId,
			senderEmail,
			msgEnt.Text,
			msgEnt.FileUrl,
			msgEnt.CreatedAt
		);
	}

	public async Task<(ChatEnt Chat, bool IsNew)> CreateOrGetPrivateChatAsync(Guid currentUserId, string targetEmail)
	{
		var targetUser = await db.Users.FirstOrDefaultAsync(u => u.Email == targetEmail);
		if (targetUser == null)
		{
			throw new InvalidOperationException("User not found");
		}

		if (targetUser.Id == currentUserId)
		{
			throw new InvalidOperationException("Self chat not allowed");
		}

		var existingChat = await db.Chats
			.Where(c => !c.IsGlobal)
			.Where(c => c.ChatUsers.Any(cu => cu.UserId == currentUserId) &&
			            c.ChatUsers.Any(cu => cu.UserId == targetUser.Id))
			.FirstOrDefaultAsync();

		if (existingChat != null)
		{
			return (existingChat, false);
		}

		var newChat = new ChatEnt
		{
			Id = Guid.NewGuid(),
			Name = targetUser.Email,
			IsGlobal = false,
		};

		db.Chats.Add(newChat);
		db.ChatUsers.Add(new ChatUserEnt { ChatId = newChat.Id, UserId = currentUserId });
		db.ChatUsers.Add(new ChatUserEnt { ChatId = newChat.Id, UserId = targetUser.Id });

		await db.SaveChangesAsync();

		return (newChat, true);
	}
}
