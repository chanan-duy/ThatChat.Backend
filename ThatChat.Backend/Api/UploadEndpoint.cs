using Microsoft.AspNetCore.Mvc;
using ThatChat.Backend.Services;

namespace ThatChat.Backend.Api;

public static class UploadEndpoint
{
	extension(RouteGroupBuilder group)
	{
		public RouteGroupBuilder MapUploadEndpoint()
		{
			group.MapPost("/upload",
					async (IFormFile? file, [FromServices] IFileUploader uploader) =>
					{
						const long maxFileSizeBytes = 20 * 1024 * 1024;

						if (file == null || file.Length == 0)
						{
							return Results.BadRequest("No file");
						}

						if (file.Length >= maxFileSizeBytes)
						{
							return Results.BadRequest($"Max file size is: {maxFileSizeBytes:N2} bytes");
						}

						var fileUrl = await uploader.UploadFileToRemote(file);

						return Results.Ok(new { Url = fileUrl });
					})
				.RequireAuthorization()
				.DisableAntiforgery();

			return group;
		}
	}
}
