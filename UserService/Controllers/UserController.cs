using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;

namespace UserService.Controllers
{
	//[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class UserController : ControllerBase
	{
		private readonly UserManager<AppUser> _userManager; // act as DB context for user
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration; // to fetch data from appsettings.json
		//private readonly IPublishEndpoint _publishEndpoint;

		public UserController(
			UserManager<AppUser> userManager,
			IHttpClientFactory httpClientFactory,
			IConfiguration configuration
		//IPublishEndpoint publishEndpoint
		)
		{
			_userManager = userManager;
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
			//_publishEndpoint = publishEndpoint;
		}

		// GET: api/user/getallusers
		[Authorize(Roles = "Admin")]
		[HttpGet("getallusers")]
		public IActionResult GetAllUsers()
		{
			var users = _userManager.Users.ToList();
			return Ok(users);
		}

		// GET: api/user/{id}
		//[Authorize(Roles = "Admin,Customer")]
		[HttpGet("{id}")]
		public async Task<IActionResult> GetUserById(string id)
		{
			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();
			return Ok(user);
		}

		// GET: api/user/products
		[HttpGet("products")]
		public async Task<IActionResult> ViewAllProducts()
		{

			using var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);
			var products = await client.GetFromJsonAsync<List<ProductDto>>("https://localhost:5001/api/products");
			return Ok(products);
		}

		// POST: api/user/buyproduct
		[HttpPost("buyproduct")]
		public async Task<IActionResult> BuyProduct(OrderRequestDto orderRequest)
		{
			// var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // get logged-in user ID

			var client = _httpClientFactory.CreateClient("OrderService");
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			if (string.IsNullOrEmpty(accessToken))
			{
				return Unauthorized("Access token is missing");
			}
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

			// Add userId to your order request if needed in DTO
			// If your OrderRequestDto includes UserId, set it here.

			var response = await client.PostAsJsonAsync("https://localhost:5002/api/orders", orderRequest);

			if (!response.IsSuccessStatusCode)
				return BadRequest("Failed to place order");

			var createdOrder = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
			return CreatedAtAction(nameof(GetOrdersByUser), new { userId = orderRequest.UserId }, createdOrder);
		}

		// GET: api/user/orders
		[HttpGet("orders/{userId}")]
		public async Task<IActionResult> GetOrdersByUser([FromRoute] Guid userId)
		{
			// var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // get logged-in user ID

			var client = _httpClientFactory.CreateClient("OrderService");

			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			if (string.IsNullOrEmpty(accessToken))
			{
				return Unauthorized("Access token is missing");
			}
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

			// Assuming your order service supports filtering orders by userId as query or route param
			var orders = await client.GetFromJsonAsync<List<OrderResponseDto>>($"https://localhost:5002/api/orders/by-user/{userId}");

			return Ok(orders);
		}

		// PUT: api/user/orders/{orderId}
		[HttpPut("orders/{orderId}")]
		public async Task<IActionResult> ModifyOrder(Guid orderId, OrderUpdateRequestDto updatedOrder)
		{
			var client = _httpClientFactory.CreateClient("OrderService");
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			if (string.IsNullOrEmpty(accessToken))
			{
				return Unauthorized("Access token is missing");
			}
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
			var response = await client.PutAsJsonAsync($"https://localhost:5002/api/orders/{orderId}", updatedOrder);

			if (!response.IsSuccessStatusCode)
				return BadRequest("Failed to update order");

			var updated = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
			return Ok(updated);
		}

		[HttpGet("users/{id}/orders-with-products")]
		public async Task<IActionResult> GetUserWithOrdersAndProducts(string id)
		{
			var user = await GetUserById(id);

			var client = _httpClientFactory.CreateClient("OrderService");
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			if (string.IsNullOrEmpty(accessToken))
			{
				return Unauthorized("Access token is missing");
			}
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

			// Assuming your order service supports filtering orders by userId as query or route param
			var orders = await client.GetFromJsonAsync<List<OrderResponseDto>>($"https://localhost:5002/api/orders/by-user/{id}");
			
			return Ok(orders);
		}

	}
}

