using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ThatChat.Backend.Data;

// Создадим позже

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("DefaultConnection") ??
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
	app.MapScalarApiReference(); // http://localhost:5000/scalar
}

app.UseCors();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth").MapIdentityApi<AppUserEnt>();

// Мапим наш будущий Хаб
// app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/", () => "Chat Backend v1.0 running");

app.Run();
