using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Data;
using UserService.Models;

namespace UserService.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly UserManager<AppUser> _userManager;
		private readonly ITokenService _tokenService;
		private readonly SignInManager<AppUser> _signInManager;
		private readonly IConfiguration _configuration;

		public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration configuration, ITokenService tokenService)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_configuration = configuration;
			_tokenService = tokenService;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterDto model)
		{
			var user = new AppUser { UserName = model.Email, Email = model.Email };
			var result = await _userManager.CreateAsync(user, model.Password);

			if (!result.Succeeded)
				return BadRequest(result.Errors);

			await _userManager.AddToRoleAsync(user, model.Role);
			return Ok("User registered.");
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDto model)
		{
			var user = await _userManager.FindByEmailAsync(model.Email);
			if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
				return Unauthorized();

			var roles = await _userManager.GetRolesAsync(user);
			//var token = GenerateJwtToken(user, roles);
			//return Ok(new { token });
			var accessToken = _tokenService.GenerateAccessToken(user, roles);
			var refreshToken = _tokenService.GenerateRefreshToken();

			user.RefreshToken = refreshToken;
			user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
			await _userManager.UpdateAsync(user);

			return Ok(new
			{
				token = accessToken,
				refreshToken = refreshToken
			});
		}




		[HttpPost("refresh")]
		public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto tokenDto)
		{
			if (tokenDto is null)
				return BadRequest("Invalid request");

			var principal = _tokenService.GetPrincipalFromExpiredToken(tokenDto.AccessToken);
			if (principal == null)
				return BadRequest("Invalid access token or refresh token");

			var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
			var user = await _userManager.FindByEmailAsync(email);

			if (user == null ||
				user.RefreshToken != tokenDto.RefreshToken ||
				user.RefreshTokenExpiryTime <= DateTime.UtcNow)
			{
				return BadRequest("Invalid refresh token");
			}

			var roles = await _userManager.GetRolesAsync(user);
			var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
			var newRefreshToken = _tokenService.GenerateRefreshToken();

			// Update refresh token in DB
			user.RefreshToken = newRefreshToken;
			user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
			await _userManager.UpdateAsync(user);

			return Ok(new
			{
				token = newAccessToken,
				refreshToken = newRefreshToken
			});
		}




		[HttpPost("logout")]
		public async Task<IActionResult> Logout()
		{
			var user = await _userManager.FindByNameAsync(User.Identity.Name);
			if (user == null) return Unauthorized();

			user.RefreshToken = null;
			user.RefreshTokenExpiryTime = DateTime.MinValue;
			await _userManager.UpdateAsync(user);

			return Ok();
		}
	}
}
