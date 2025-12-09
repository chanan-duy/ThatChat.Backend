namespace ThatChat.Backend.Services;

public interface IFileUploader
{
	Task<string> UploadFileToRemote(IFormFile file);
}
