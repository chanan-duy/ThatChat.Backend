using System.Text;

namespace ThatChat.Backend.Services;

public class LogMiddlewareService : IMiddleware
{
	private static readonly SemaphoreSlim FileLock = new(1, 1);

	public LogMiddlewareService()
	{
		if (!Directory.Exists("Logs"))
		{
			Directory.CreateDirectory("Logs");
		}
	}

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		var ip = context.Connection.RemoteIpAddress?.ToString() ?? "-";
		var user = context.User.Identity?.Name ?? "-";

		var method = context.Request.Method;
		var path = context.Request.Path;
		var protocol = context.Request.Protocol;

		await next(context);

		var status = context.Response.StatusCode;
		var time = DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss zzz");

		var logLine = $"{ip} - {user} [{time}] \"{method} {path} {protocol}\" {status} -\n";

		var currentFileName = $"access-{DateTime.Now:yyyy-MM-dd}.log";
		var logPath = Path.Combine("Logs", currentFileName);

		await FileLock.WaitAsync();
		try
		{
			await File.AppendAllTextAsync(logPath, logLine, Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to write log: {ex.Message}");
		}
		finally
		{
			FileLock.Release();
		}
	}
}
