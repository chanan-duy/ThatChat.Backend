using ThatChat.Backend.Data;
using ThatChat.Backend.Dto;

namespace ThatChat.Backend.Services;

public interface IChatService
{
	Task<bool> HasAccessToChatAsync(Guid userId, Guid chatId);
	Task<ChatMessageDto?> SaveMessageAsync(Guid userId, Guid chatId, string message, string? fileUrl);
	Task<(ChatEnt Chat, bool IsNew)> CreateOrGetPrivateChatAsync(Guid currentUserId, string targetEmail);
}
