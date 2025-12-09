using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Allure.NUnit;
using FluentAssertions;
using ThatChat.Backend.Dto;
using ThatChat.Backend.Tests.Infra;

namespace ThatChat.Backend.Tests.Api;

[AllureNUnit]
public class ChatApiTests
{
	private CustomWebApplicationFactory _factory;
	private HttpClient _client;

	[SetUp]
	public void Setup()
	{
		_factory = new CustomWebApplicationFactory();
		_client = _factory.CreateClient();
	}

	[TearDown]
	public void TearDown()
	{
		_client.Dispose();
		_factory.Dispose();
	}

	private async Task<string> RegisterAndLogin(string email, string password)
	{
		var regResponse = await _client.PostAsJsonAsync("/api/auth/register", new { email, password });
		regResponse.EnsureSuccessStatusCode();

		var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
		loginResponse.EnsureSuccessStatusCode();

		var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
		return loginResult!.AccessToken;
	}

	private record LoginResult(string AccessToken, string RefreshToken, int ExpiresIn);

	[Test]
	public async Task GetChats_ShouldReturnGlobalChat_ForNewUser()
	{
		var token = await RegisterAndLogin("newuser@test.com", "pass1234");
		_client.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", token);

		var response = await _client.GetAsync("/api/chats");

		response.EnsureSuccessStatusCode();
		var chats = await response.Content.ReadFromJsonAsync<List<ChatDto>>();

		chats.Should().Contain(c => c.IsGlobal == true);
	}

	[Test]
	public async Task CreateChat_ShouldReturn200_WhenUserExists()
	{
		var tokenA = await RegisterAndLogin("userA@test.com", "pass1234");

		await _client.PostAsJsonAsync("/api/auth/register", new { email = "userB@test.com", password = "pass1234" });

		_client.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", tokenA);

		var response = await _client.PostAsJsonAsync("/api/chats", new CreateChatRequest("userB@test.com"));

		response.EnsureSuccessStatusCode();
		var chat = await response.Content.ReadFromJsonAsync<ChatDto>();
		chat.Should().NotBeNull();
		chat.Name.Should().Be("userB@test.com");
	}

	[Test]
	public async Task Upload_ShouldFail_WhenFileIsTooLarge()
	{
		var token = await RegisterAndLogin("uploader@test.com", "pass1234");
		_client.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", token);

		var largeContent = new ByteArrayContent(new byte[20 * 1024 * 1024 + 1]);
		var multipart = new MultipartFormDataContent();
		multipart.Add(largeContent, "file", "large.png");

		var response = await _client.PostAsync("/api/upload", multipart);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Test]
	public async Task Middleware_ShouldHandleConcurrentRequests()
	{
		var tasks = new List<Task>();
		for (var i = 0; i < 50; i++)
		{
			tasks.Add(_client.GetAsync("/"));
		}

		await Task.WhenAll(tasks);

		var logsExist = Directory.GetFiles("Logs", "access-*.log").Length != 0;
		logsExist.Should().BeTrue();
	}
}
