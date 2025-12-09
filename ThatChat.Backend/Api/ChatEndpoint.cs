using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThatChat.Backend.Data;
using ThatChat.Backend.Dto;
using ThatChat.Backend.Services;

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


			group.MapPost("/",
					async (CreateChatRequest req, AppDbContext db, ClaimsPrincipal user, [FromServices] IChatService chatService) =>
					{
						var currentUserId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

						var dto = await chatService.CreateOrGetPrivateChatAsync(currentUserId, req.Email);

						return Results.Ok(new { dto.Chat.Id, dto.Chat.Name, dto.Chat.IsGlobal });
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
