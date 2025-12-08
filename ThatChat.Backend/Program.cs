using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ThatChat.Backend.Data;
using ThatChat.Backend.Hubs;

// Создадим позже

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("AppDbContext") ??
                 throw new InvalidOperationException("Connection string 'AppDbContext' not found.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connString));

builder.Services.AddAuthentication()
	.AddBearerToken(IdentityConstants.BearerScheme);

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<AppUserEnt>(options =>
	{
		options.Password.RequireDigit = false;
		options.Password.RequireLowercase = false;
		options.Password.RequireNonAlphanumeric = false;
		options.Password.RequireUppercase = false;
		options.Password.RequiredLength = 4;

		options.User.RequireUniqueEmail = true;
	})
	.AddEntityFrameworkStores<AppDbContext>()
	.AddApiEndpoints();

builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, options =>
{
	options.BearerTokenExpiration = TimeSpan.FromDays(7);

	options.Events = new BearerTokenEvents
	{
		OnMessageReceived = context =>
		{
			var accessToken = context.Request.Query["access_token"];

			var path = context.HttpContext.Request.Path;
			if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
			{
				context.Token = accessToken;
			}

			return Task.CompletedTask;
		},
	};
});

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.WithOrigins("http://localhost:5173")
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});

builder.Services.AddOpenApi();

builder.Services.AddSignalR();

var app = builder.Build();

app.Use(async (context, next) =>
{
	var ip = context.Connection.RemoteIpAddress?.ToString() ?? "-";
	var user = context.User.Identity?.Name ?? "-";
	var time = DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss zzz");
	var method = context.Request.Method;
	var path = context.Request.Path;
	var protocol = context.Request.Protocol;

	await next();

	var status = context.Response.StatusCode;
	var logLine = $"{ip} - {user} [{time}] \"{method} {path} {protocol}\" {status} -\n";

	await File.AppendAllTextAsync("access.log", logLine);
});

if (app.Environment.IsDevelopment())
{
	using (var scope = app.Services.CreateScope())
	{
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		db.Database.Migrate();
	}

	app.MapOpenApi();
	app.MapScalarApiReference(); // http://localhost:5042/scalar
}

app.UseCors();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth").MapIdentityApi<AppUserEnt>();

app.MapPost("/api/upload", async (IFormFile file) =>
	{
		if (file == null || file.Length == 0)
		{
			return Results.BadRequest("No file uploaded");
		}

		var ext = Path.GetExtension(file.FileName);
		var newName = $"{Guid.NewGuid()}{ext}";

		var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
		if (!Directory.Exists(uploadPath))
		{
			Directory.CreateDirectory(uploadPath);
		}

		var fullPath = Path.Combine(uploadPath, newName);

		await using (var stream = new FileStream(fullPath, FileMode.Create))
		{
			await file.CopyToAsync(stream);
		}

		var fileUrl = $"/uploads/{newName}";
		return Results.Ok(new { Url = fileUrl });
	})
	.RequireAuthorization();

app.MapGet("/api/chats", async (AppDbContext db, ClaimsPrincipal user) =>
	{
		var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

		var chats = await db.Chats
			.Include(c => c.ChatUsers)
			.Where(c => c.IsGlobal || c.ChatUsers.Any(cu => cu.UserId == userId))
			.Select(c => new
			{
				c.Id,
				c.Name,
				c.IsGlobal,
			})
			.ToListAsync();

		return Results.Ok(chats);
	})
	.RequireAuthorization();

app.MapGet("/api/chats/{chatId:guid}/messages", async (Guid chatId, AppDbContext db) =>
	{
		var messages = await db.Messages
			.Where(m => m.ChatId == chatId)
			.OrderBy(m => m.CreatedAt)
			.Select(m => new
			{
				m.Id,
				m.ChatId,
				m.SenderId,
				SenderEmail = m.Sender.Email,
				m.Text,
				m.FileUrl,
				m.CreatedAt,
			})
			.ToListAsync();

		return Results.Ok(messages);
	})
	.RequireAuthorization();


app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/", () => "Running");

app.Run();
