namespace ThatChat.Backend.Data;

public class MessageEnt
{
	public Guid Id { get; set; }
	public string? Text { get; set; }
	public string? FileUrl { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public Guid ChatId { get; set; }
	public ChatEnt Chat { get; set; } = null!;

	public Guid SenderId { get; set; }
	public AppUserEnt Sender { get; set; } = null!;
}
