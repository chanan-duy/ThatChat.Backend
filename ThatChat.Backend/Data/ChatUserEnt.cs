namespace ThatChat.Backend.Data;

public class ChatUserEnt
{
	public Guid UserId { get; set; }
	public AppUserEnt User { get; set; } = null!;

	public Guid ChatId { get; set; }
	public ChatEnt Chat { get; set; } = null!;
}
