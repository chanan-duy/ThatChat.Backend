using System.Net.Http.Headers;
using System.Net.Http.Json;
using Allure.NUnit;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using ThatChat.Backend.Data;
using ThatChat.Backend.Dto;
using ThatChat.Backend.Tests.Infra;

namespace ThatChat.Backend.Tests.Hubs;

[AllureNUnit]
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

	[Test]
	public async Task JoinChat_ShouldNotReceiveMessages_IfUserHasNoAccess()
	{
		var client = _factory.CreateClient();

		var hackerToken = await GetAccessToken(client, "hacker@test.com");
		var victimToken = await GetAccessToken(client, "victim@test.com");

		await client.PostAsJsonAsync("/api/auth/register", new { email = "other@test.com", password = "password" });

		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", victimToken);

		var chatRes = await client.PostAsJsonAsync("/api/chats", new CreateChatRequest("other@test.com"));

		chatRes.EnsureSuccessStatusCode();

		var chat = await chatRes.Content.ReadFromJsonAsync<ChatDto>();

		var hackerConnection = new HubConnectionBuilder()
			.WithUrl("http://localhost/hubs/chat", o =>
			{
				o.AccessTokenProvider = () => Task.FromResult<string?>(hackerToken);
				o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
			})
			.Build();

		var receivedMessages = new List<ChatMessageDto>();
		hackerConnection.On<ChatMessageDto>("ReceiveChatMessage", msg => receivedMessages.Add(msg));
		await hackerConnection.StartAsync();

		await hackerConnection.InvokeAsync("JoinChat", chat!.Id);

		var victimConnection = new HubConnectionBuilder()
			.WithUrl("http://localhost/hubs/chat", o =>
			{
				o.AccessTokenProvider = () => Task.FromResult<string?>(victimToken);
				o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
			})
			.Build();

		await victimConnection.StartAsync();
		await victimConnection.InvokeAsync("SendMessage", chat.Id, "Secret Message", null);

		await Task.Delay(200);

		receivedMessages.Should().BeEmpty();

		await hackerConnection.DisposeAsync();
		await victimConnection.DisposeAsync();
	}

	[Test]
	public async Task CreateChat_ShouldNotifyTargetUser_ViaSignalR()
	{
		var client = _factory.CreateClient();

		var senderToken = await GetAccessToken(client, "sender@test.com");
		var receiverToken = await GetAccessToken(client, "receiver@test.com");

		var receiverConnection = new HubConnectionBuilder()
			.WithUrl("http://localhost/hubs/chat", o =>
			{
				o.AccessTokenProvider = () => Task.FromResult<string?>(receiverToken);
				o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
			})
			.Build();

		var receivedChats = new List<ChatDto>();
		receiverConnection.On<ChatDto>("ReceiveNewChat", chat => receivedChats.Add(chat));
		await receiverConnection.StartAsync();

		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", senderToken);

		var response = await client.PostAsJsonAsync("/api/chats", new CreateChatRequest("receiver@test.com"));
		response.EnsureSuccessStatusCode();

		var timeout = DateTime.Now.AddSeconds(2);
		while (receivedChats.Count == 0 && DateTime.Now < timeout)
		{
			await Task.Delay(50);
		}

		receivedChats.Should().ContainSingle();

		receivedChats[0].Name.Should().Be("receiver@test.com");

		await receiverConnection.DisposeAsync();
	}
}
