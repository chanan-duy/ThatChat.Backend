using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ThatChat.Backend.Data;

public class AppDbContext : IdentityDbContext<AppUserEnt, IdentityRole<Guid>, Guid>
{
	public static readonly Guid GlobalChatId = Guid.Parse("11111111-1111-1111-1111-111111111111");

	public DbSet<ChatEnt> Chats { get; set; }
	public DbSet<MessageEnt> Messages { get; set; }
	public DbSet<ChatUserEnt> ChatUsers { get; set; }

	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<ChatUserEnt>()
			.HasKey(cu => new { cu.ChatId, cu.UserId });

		modelBuilder.Entity<ChatEnt>().HasData(
			new ChatEnt
			{
				Id = GlobalChatId,
				Name = "Общий Чат",
				IsGlobal = true,
			}
		);

		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
				{
					property.SetValueConverter(new DateTimeUtcConverter());
				}
			}
		}
	}
}
