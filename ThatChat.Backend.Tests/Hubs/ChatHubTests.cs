using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using ThatChat.Backend.Data;
using ThatChat.Backend.Dto;
using ThatChat.Backend.Tests.Infra;

namespace ThatChat.Backend.Tests.Hubs;

public class ChatHubTests
{
	private CustomWebApplicationFactory _factory;

	[SetUp]
	public void Setup()
	{
		_factory = new CustomWebApplicationFactory();
	}

	[TearDown]
	public void TearDown()
	{
		_factory.Dispose();
	}

	private static async Task<string> GetAccessToken(HttpClient client, string email)
	{
		await client.PostAsJsonAsync("/api/auth/register", new { email, password = "password" });
		var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "password" });
		var data = await res.Content.ReadFromJsonAsync<LoginResult>();
		return data!.AccessToken;
	}

	private record LoginResult(string AccessToken);

	[Test]
	public async Task SendMessage_ShouldBroadcastToGroup()
	{
		var client = _factory.CreateClient();
		var token = await GetAccessToken(client, "hubuser@test.com");

		var hubConnection = new HubConnectionBuilder()
			.WithUrl("http://localhost/hubs/chat", options =>
			{
				options.AccessTokenProvider = () => Task.FromResult<string?>(token);
				options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
			})
			.Build();

		var receivedMessages = new List<ChatMessageDto>();
		hubConnection.On<ChatMessageDto>("ReceiveChatMessage", msg => { receivedMessages.Add(msg); });

		await hubConnection.StartAsync();

		var globalChatId = AppDbContext.GlobalChatId;
		await hubConnection.InvokeAsync("JoinChat", globalChatId);

		const string messageText = "Hello SignalR";
		await hubConnection.InvokeAsync("SendMessage", globalChatId, messageText, null);

		var timeout = DateTime.Now.AddSeconds(2);
		while (receivedMessages.Count == 0 && DateTime.Now < timeout)
		{
			await Task.Delay(100);
		}

		receivedMessages.Should().ContainSingle();
		receivedMessages[0].Text.Should().Be(messageText);
		receivedMessages[0].SenderEmail.Should().Be("hubuser@test.com");
	}
}
