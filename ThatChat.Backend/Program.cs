using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ThatChat.Backend.Api;
using ThatChat.Backend.Data;
using ThatChat.Backend.Hubs;
using ThatChat.Backend.Services;

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

var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ??
                  throw new InvalidOperationException("String 'CorsOrigins' not found.");
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.WithOrigins(corsOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});

builder.Services.AddOpenApi();

builder.Services.AddSignalR();

builder.Services.AddScoped<LogMiddlewareService>();

var app = builder.Build();

app.UseMiddleware<LogMiddlewareService>();

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

app.MapGet("/", () => "Running");

app.MapGroup("/api").MapApi();

app.MapHub<ChatHub>("/hubs/chat");

app.Run();
