namespace ThatChat.Backend.Api;

public static class ApiMain
{
	extension(RouteGroupBuilder group)
	{
		public RouteGroupBuilder MapApi()
		{
			group.MapGroup("/auth").MapAuthEndpoint().WithTags("Auth");
			group.MapUploadEndpoint().WithTags("Upload");
			group.MapGroup("/chats").MapChatEndpoint().WithTags("Chat");

			return group;
		}
	}
}
