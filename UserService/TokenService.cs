using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UserService.Models;

namespace UserService
{
	public class TokenService : ITokenService
	{
		private readonly IConfiguration _configuration;

		public TokenService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public string GenerateAccessToken(AppUser user, IList<string> roles)
		{
			var claims = new List<Claim>
			{
				//new Claim(ClaimTypes.NameIdentifier, user.Id),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Name, user.UserName)
			};

			claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
			Console.WriteLine(key.ToString());
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
			var token = new JwtSecurityToken(
				issuer: _configuration["Jwt:Issuer"],
				audience: _configuration["Jwt:Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddHours(1),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		/// <summary>
		/// Generates a cryptographically secure refresh token as a base64 string.
		/// </summary>
		/// <returns>A 512-bit (64-byte) randomly generated refresh token string.</returns>
		public string GenerateRefreshToken()
		{
			// Create a 512-bit (64-byte) array for secure random data
			var randomBytes = new byte[64];

			// Use a cryptographic random number generator
			using var rng = RandomNumberGenerator.Create();
			rng.GetBytes(randomBytes);

			// Convert the random bytes to a Base64 string for storage
			return Convert.ToBase64String(randomBytes);
		}



		// helper method to extract expired token’s claims:
		public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
		{
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateAudience = true,
				ValidateIssuer = true,
				ValidAudience = _configuration["Jwt:Audience"],
				ValidIssuer = _configuration["Jwt:Issuer"],
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
				ValidateLifetime = false // Important: we're validating *expired* tokens
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			SecurityToken securityToken;

			var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
			return principal;
		}
	}

}
