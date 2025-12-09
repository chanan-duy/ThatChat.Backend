using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ThatChat.Backend.Data;

public class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
{
	public DateTimeUtcConverter()
		: base(
			d => d.ToUniversalTime(),
			d => DateTime.SpecifyKind(d, DateTimeKind.Utc))
	{
	}
}
