using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThatChat.Backend.Data;

namespace ThatChat.Backend.Tests.Infra;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.ConfigureServices(services =>
		{
			var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

			if (dbContextDescriptor != null)
			{
				services.Remove(dbContextDescriptor);
			}

			var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));

			if (dbConnectionDescriptor != null)
			{
				services.Remove(dbConnectionDescriptor);
			}

			services.AddSingleton<DbConnection>(container =>
			{
				var connection = new SqliteConnection("DataSource=:memory:");
				connection.Open();
				return connection;
			});

			services.AddDbContext<AppDbContext>((container, options) =>
			{
				var connection = container.GetRequiredService<DbConnection>();
				options.UseSqlite(connection);
			});
		});
	}
}
