using System.Text.Json.Serialization;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace ThatChat.Backend.Data;

public class ChatEnt
{
	public Guid Id { get; set; }
	public string? Name { get; set; }
	public bool IsGlobal { get; set; }

	[JsonIgnore] public List<ChatUserEnt> ChatUsers { get; set; } = [];
	[JsonIgnore] public List<MessageEnt> Messages { get; set; } = [];
}
