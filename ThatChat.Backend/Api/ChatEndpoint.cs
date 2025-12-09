using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ThatChat.Backend.Data;
using ThatChat.Backend.Dto;

namespace ThatChat.Backend.Api;

public static class ChatEndpoint
{
	extension(RouteGroupBuilder group)
	{
		public RouteGroupBuilder MapChatEndpoint()
		{
			group.MapGet("/", async (AppDbContext db, ClaimsPrincipal user) =>
				{
					var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

					var chats = await db.Chats
						.Include(c => c.ChatUsers)
						.Where(c => c.IsGlobal || c.ChatUsers.Any(cu => cu.UserId == userId))
						.Select(c => new
						{
							c.Id,
							c.Name,
							c.IsGlobal,
						})
						.ToListAsync();

					return Results.Ok(chats);
				})
				.RequireAuthorization();


			group.MapPost("/", async (CreateChatRequest req, AppDbContext db, ClaimsPrincipal user) =>
				{
					var currentUserId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

					var targetUser = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
					if (targetUser == null)
					{
						return Results.NotFound("Пользователь с таким Email не найден");
					}

					if (targetUser.Id == currentUserId)
					{
						return Results.BadRequest("Нельзя создать чат с самим собой");
					}

					var existingChat = await db.Chats
						.Where(c => !c.IsGlobal)
						.Where(c => c.ChatUsers.Any(cu => cu.UserId == currentUserId) &&
						            c.ChatUsers.Any(cu => cu.UserId == targetUser.Id))
						.FirstOrDefaultAsync();

					if (existingChat != null)
					{
						return Results.Ok(new { existingChat.Id, existingChat.Name, existingChat.IsGlobal });
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

					return Results.Ok(new { newChat.Id, newChat.Name, newChat.IsGlobal });
				})
				.RequireAuthorization();

			group.MapGet("/{chatId:guid}/messages", async (Guid chatId, AppDbContext db) =>
				{
					var messages = await db.Messages
						.Where(m => m.ChatId == chatId)
						.OrderBy(m => m.CreatedAt)
						.Select(m => new
						{
							m.Id,
							m.ChatId,
							m.SenderId,
							SenderEmail = m.Sender.Email,
							m.Text,
							m.FileUrl,
							m.CreatedAt,
						})
						.ToListAsync();

					return Results.Ok(messages);
				})
				.RequireAuthorization();

			return group;
		}
	}
}
