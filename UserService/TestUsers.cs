using Duende.IdentityServer.Test;
using System.Collections.Generic;
using System.Security.Claims;

namespace UserService
{
	public static class TestUsers
	{
		public static List<TestUser> Users =>
			new List<TestUser>
			{
			new TestUser
			{
				SubjectId = "1",
				Username = "testuser",
				Password = "password",
				Claims = new List<Claim>
				{
					new Claim("name", "Test User"),
					new Claim("website", "https://example.com")
				}
			}
			};
	}
}
