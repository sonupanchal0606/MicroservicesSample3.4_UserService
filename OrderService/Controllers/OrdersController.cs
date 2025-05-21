using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using Shared.Messages;
using System.Net.Http.Headers;

namespace OrderService.Controllers
{
	//[Authorize(Roles = "Customer")]
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class OrdersController : ControllerBase
	{

		private readonly OrderDbContext _context; // for DB connection
		private readonly IHttpClientFactory _httpClientFactory; // to call another service api end point 
		private readonly IPublishEndpoint _publishEndpoint; // to use RabbitMQ message queue for publishing event
		private readonly IConfiguration _configuration; // to fetch data from appsettings.json

		// private readonly HttpClient _httpClient; to use http client from appsettings.json
		public OrdersController(OrderDbContext context, IHttpClientFactory httpClientFactory, IPublishEndpoint publishEndpoint, IConfiguration configuration)
		{
			_context = context;
			_httpClientFactory = httpClientFactory;
			_publishEndpoint = publishEndpoint;
			_configuration = configuration;
		}


		[HttpGet]
		public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrders()
		{
			var orderItems = await _context.Orders.ToListAsync();
			var response = new List<OrderResponseDto>();

			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]); // add this if you have API key

			foreach (var order in orderItems)
			{
				// Call ProductService for product info
				var product = await client.GetFromJsonAsync<ProductDto>($"https://localhost:5001/api/products/{order.ProductId}");

				response.Add(new OrderResponseDto
				{
					Id = order.Id,
					Quantity = order.Quantity,
					TotalPrice = order.TotalPrice,
					Product = product
				});
			}

			return Ok(response);
		}


		[HttpGet("{id}")]
		public async Task<IActionResult> Get(Guid id)
		{
			var orderItem = await _context.Orders.FindAsync(id);
			if (orderItem == null)
				return NotFound();

			using var client = _httpClientFactory.CreateClient("ProductService");
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);
			var product = await client.GetFromJsonAsync<ProductDto>($"https://localhost:5001/api/products/{orderItem.ProductId}");

			if (product == null)
				return NotFound();

			var OrdeDto = new OrderResponseDto
			{
				Id = orderItem.Id,
				Quantity = orderItem.Quantity,
				TotalPrice = orderItem.TotalPrice,
				Product = product
			};

			return Ok(orderItem);
		}

		[HttpPost]
		public async Task<IActionResult> Create(OrderRequestDto orderPost)
		{
			using var client = _httpClientFactory.CreateClient("ProductService");
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);

			var product = await client.GetFromJsonAsync<ProductDto>($"https://localhost:5001/api/products/{orderPost.ProductId}");

			if (product == null)
				return NotFound("Product not found");

			if (product.Quantity < orderPost.Quantity)
				return BadRequest("insufficient product quantity.");

			// you can use event queue here for message passing instead of API calls here
			// var reduceResponse = await client.PutAsync($"https://localhost:5001/api/products/reduce-quantity/{orderPost.ProductId}?quantity={orderPost.Quantity}", null);

			Order order = new Order();
			order.Id = Guid.NewGuid();
			order.Quantity = orderPost.Quantity;
			order.UserId = orderPost.UserId;
			order.ProductId = orderPost.ProductId;
			order.TotalPrice = product.Price * orderPost.Quantity;

			_context.Orders.Add(order);
			await _context.SaveChangesAsync();

			// using events instead of HTTP calls
			await _publishEndpoint.Publish(new OrderCreated
			{
				ProductId = order.ProductId,
				Quantity = order.Quantity
			});

			return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
		}




		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateOrder(Guid id, OrderUpdateRequestDto updatedOrder)
		{
			var order = await _context.Orders.FindAsync(id);
			if (order == null) return NotFound("Order not found");

			var pricePerItem = order.TotalPrice / order.Quantity;

			var diffQty = updatedOrder.Quantity - order.Quantity;

			var client = _httpClientFactory.CreateClient("ProductService");
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			if (string.IsNullOrEmpty(accessToken))
			{
				return Unauthorized("Access token is missing");
			}
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

			// you can use event queue here for message passing instead of API calls here.
			// but first you will query product for the quantoty and then send message queues. instead modify product in one http call only
			var response = await client.PutAsync($"https://localhost:5001/api/products/adjust-quantity/{order.ProductId}?difference={diffQty}", null);

			if (!response.IsSuccessStatusCode)
				return BadRequest("Failed to adjust product quantity");

			order.Quantity = updatedOrder.Quantity;
			order.TotalPrice = updatedOrder.Quantity * pricePerItem;

			await _context.SaveChangesAsync();
			return Ok(order);
		}

	

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteOrder(Guid id)
		{
			var order = await _context.Orders.FindAsync(id);
			if (order == null) return NotFound();

			_context.Orders.Remove(order);
			await _context.SaveChangesAsync();

			// Publish OrderDeleted event
			var message = new OrderDeleted
			{
				ProductId = order.ProductId,
				Quantity = order.Quantity
			};
			await _publishEndpoint.Publish(message);

			return NoContent();
		}

		[HttpGet("all-orders-with-users")]
		public async Task<IActionResult> GetAllUsersWithOrders()
		{

			using var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);
			var accessToken = await HttpContext.GetTokenAsync("access_token");
			if (string.IsNullOrEmpty(accessToken))
			{
				return Unauthorized("Access token is missing");
			}
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
			var orders = await _context.Orders.ToListAsync();
			var result = new List<object>();

			foreach (var order in orders)
			{
				var user = await client.GetFromJsonAsync<UserDto>($"https://localhost:5000/api/User/{order.UserId}");
				var product = await client.GetFromJsonAsync<ProductDto>($"https://localhost:5001/api/products/{order.ProductId}");

				result.Add(new
				{
					OrderId = order.Id,
					Quantity = order?.Quantity ?? 0,
					TotalPrice = order?.TotalPrice ?? 0,
					User = user,
					Products = product,
				});
			}

			return Ok(result);
		}


		[HttpGet("by-user/{userId}")]
		public async Task<IActionResult> GetOrdersByUser(Guid userId)
		{
			var orders = await _context.Orders
				.Where(o => o.UserId == userId)
				.ToListAsync();

			using var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Add("x-api-key", _configuration["ApiKey"]);

			var result = new List<OrderResponseDto>();

			foreach (var order in orders)
			{

				var user = await client.GetFromJsonAsync<UserDto>($"https://localhost:5000/api/User/{order.UserId}");
				var product = await client.GetFromJsonAsync<ProductDto>($"https://localhost:5001/api/products/{order.ProductId}");

				result.Add(new OrderResponseDto
				{
					Id = order.Id,
					UserId = order.UserId,
					Quantity = order.Quantity,
					TotalPrice = order.TotalPrice,
					User = user,
					Product = product,
				});
			}

			return Ok(result);
		}

	}
}


/*
 [HttpPut("{id}")]
public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] Order updatedOrder)
{
    var existingOrder = await _dbContext.OrderItems.FindAsync(id);
    if (existingOrder == null)
        return NotFound("Order not found");

    var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == updatedOrder.ProductId);
    if (product == null)
        return BadRequest("Product not found");

    int quantityDiff = updatedOrder.Quantity - existingOrder.Quantity;
    if (product.Quantity < quantityDiff)
        return BadRequest("Not enough stock to increase orderItem quantity.");

    product.Quantity -= quantityDiff;

    existingOrder.ProductId = updatedOrder.ProductId;
    existingOrder.Quantity = updatedOrder.Quantity;
    existingOrder.TotalPrice = product.Price * updatedOrder.Quantity;

    await _dbContext.SaveChangesAsync();

    // Publish OrderUpdated event
    var message = new OrderUpdated
    {
        ProductId = updatedOrder.ProductId,
        QuantityDifference = quantityDiff
    };
    await _publishEndpoint.Publish(message);

    return Ok(existingOrder);
}

 */


/*
 [HttpDelete("{id}")]
public async Task<IActionResult> DeleteOrder(Guid id)
{
    var existingOrder = await _dbContext.OrderItems.FindAsync(id);
    if (existingOrder == null)
        return NotFound("Order not found");

    var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == existingOrder.ProductId);
    if (product != null)
        product.Quantity += existingOrder.Quantity;

    _dbContext.OrderItems.Remove(existingOrder);
    await _dbContext.SaveChangesAsync();

    // 🔔 Publish OrderDeleted event
    var message = new OrderDeleted
    {
        ProductId = existingOrder.ProductId,
        Quantity = existingOrder.Quantity
    };
    await _publishEndpoint.Publish(message);

    return NoContent();
}

 */