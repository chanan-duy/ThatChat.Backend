namespace ThatChat.Backend.Api;

public static class UploadEndpoint
{
	extension(RouteGroupBuilder group)
	{
		public RouteGroupBuilder MapUploadEndpoint()
		{
			group.MapPost("/upload", async (IFormFile? file) =>
				{
					if (file == null || file.Length == 0)
					{
						return Results.BadRequest("No file uploaded");
					}

					var ext = Path.GetExtension(file.FileName);
					var newName = $"{Guid.CreateVersion7()}{ext}";

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
				.RequireAuthorization()
				.DisableAntiforgery();

			return group;
		}
	}
}
