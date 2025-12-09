using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ThatChat.Backend.Data;
using ThatChat.Backend.Services;

namespace ThatChat.Backend.Tests.Services;

public class ChatServiceTests
{
	private AppDbContext _context;
	private ChatService _service;
	private SqliteConnection _connection;

	[SetUp]
	public void Setup()
	{
		_connection = new SqliteConnection("Filename=:memory:");
		_connection.Open();

		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(_connection)
			.Options;

		_context = new AppDbContext(options);
		_context.Database.EnsureCreated();

		_service = new ChatService(_context);
	}

	[TearDown]
	public void TearDown()
	{
		_context.Dispose();
		_connection.Dispose();
	}

	[Test]
	public async Task CreateOrGetPrivateChat_ShouldCreateNew_WhenNoneExists()
	{
		var user1 = new AppUserEnt { Id = Guid.NewGuid(), Email = "a@test.com", UserName = "a@test.com" };
		var user2 = new AppUserEnt { Id = Guid.NewGuid(), Email = "b@test.com", UserName = "b@test.com" };
		_context.Users.AddRange(user1, user2);
		await _context.SaveChangesAsync();

		var result = await _service.CreateOrGetPrivateChatAsync(user1.Id, "b@test.com");

		result.IsNew.Should().BeTrue();
		result.Chat.Should().NotBeNull();
		result.Chat.ChatUsers.Should().HaveCount(2);
	}

	[Test]
	public async Task SaveMessage_ShouldFail_IfUserIsNotInChat()
	{
		var user1 = new AppUserEnt { Id = Guid.NewGuid(), Email = "a@test.com", UserName = "a@test.com" };
		var chat = new ChatEnt { Id = Guid.NewGuid(), IsGlobal = false };

		_context.Users.Add(user1);
		_context.Chats.Add(chat);
		await _context.SaveChangesAsync();

		var result = await _service.SaveMessageAsync(user1.Id, chat.Id, "Hello", null);

		result.Should().BeNull();
	}
}
