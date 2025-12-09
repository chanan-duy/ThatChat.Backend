using ThatChat.Backend.Data;

namespace ThatChat.Backend.Api;

public static class AuthEndpoint
{
	extension(RouteGroupBuilder group)
	{
		public RouteGroupBuilder MapAuthEndpoint()
		{
			group.MapIdentityApi<AppUserEnt>();

			return group;
		}
	}
}
