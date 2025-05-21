using System.Security.Claims;
using UserService.Models;

namespace UserService
{
	public interface ITokenService
	{
		string GenerateAccessToken(AppUser user, IList<string> roles);
		string GenerateRefreshToken();
		ClaimsPrincipal GetPrincipalFromExpiredToken(string token); // For refresh flow
	}
}
