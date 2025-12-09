using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ThatChat.Backend.Data;
using ThatChat.Backend.Dto;
using ThatChat.Backend.Hubs;
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
						.Select(c => new ChatDto(c.Id, c.Name, c.IsGlobal))
						.ToListAsync();

					return Results.Ok(chats);
				})
				.RequireAuthorization();


			group.MapPost("/",
					async (
						CreateChatRequest req,
						ClaimsPrincipal user,
						[FromServices] IChatService chatService,
						[FromServices] IHubContext<ChatHub, IChatClient> hubContext
					) =>
					{
						var currentUserId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

						var (chatDto, isNew) = await chatService.CreateOrGetPrivateChatAsync(currentUserId, req.Email);

						if (isNew)
						{
							var usersToNotify = chatDto.ChatUsers
								.Select(x => x.UserId)
								.Where(x => x != currentUserId)
								.Select(x => x.ToString());

							await hubContext.Clients
								.Users(usersToNotify)
								.ReceiveNewChat(new ChatDto(chatDto.Id, chatDto.Name, chatDto.IsGlobal));
						}

						return Results.Ok(new { chatDto.Id, chatDto.Name, chatDto.IsGlobal });
					})
				.RequireAuthorization();

			group.MapGet("/{chatId:guid}/messages", async (Guid chatId, AppDbContext db) =>
				{
					var messages = await db.Messages
						.Where(m => m.ChatId == chatId)
						.OrderBy(m => m.CreatedAt)
						.Select(m => new ChatMessageDto(
							m.Id,
							m.ChatId,
							m.SenderId,
							m.Sender.Email,
							m.Text,
							m.FileUrl,
							m.CreatedAt
						))
						.ToListAsync();

					return Results.Ok(messages);
				})
				.RequireAuthorization();

			return group;
		}
	}
}
