namespace ThatChat.Backend.Services;

public class FileUploaderService(ILogger<FileUploaderService> logger) : IFileUploader
{
	public async Task<string> UploadFileToRemote(IFormFile file)
	{
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

		return fileUrl;
	}
}
