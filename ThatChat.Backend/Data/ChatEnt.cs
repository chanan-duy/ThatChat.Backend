using System.Text.Json.Serialization;

namespace ThatChat.Backend.Data;

public class ChatEnt
{
	public Guid Id { get; set; }
	public string? Name { get; set; }
	public bool IsGlobal { get; set; } = false;

	[JsonIgnore] public List<ChatUserEnt> ChatUsers { get; set; } = new();
	[JsonIgnore] public List<MessageEnt> Messages { get; set; } = new();
}
