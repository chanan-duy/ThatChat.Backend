using JetBrains.Annotations;

namespace ThatChat.Backend.Dto;

[UsedImplicitly]
public record CreateChatRequest(string Email);

[UsedImplicitly]
public record ChatMessageDto(
	Guid Id,
	Guid ChatId,
	Guid SenderId,
	string? SenderEmail,
	string? Text,
	string? FileUrl,
	DateTime CreatedAt
);
