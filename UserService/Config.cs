using Duende.IdentityServer.Models;
using System.Collections.Generic;
namespace UserService
{
	public static class Config
	{
		public static IEnumerable<IdentityResource> IdentityResources =>
			new IdentityResource[]
			{
			new IdentityResources.OpenId(),
			new IdentityResources.Profile()
			};

		public static IEnumerable<ApiScope> ApiScopes =>
			new ApiScope[]
			{
			new ApiScope("order_api", "Order API")
			};

		public static IEnumerable<Client> Clients =>
			new Client[]
			{
			new Client
			{
				ClientId = "orders_client",
				AllowedGrantTypes = GrantTypes.ClientCredentials,
				ClientSecrets =
				{
					new Secret("secret".Sha256())
				},
				AllowedScopes = { "order_api" }
			}
			};
	}

}
