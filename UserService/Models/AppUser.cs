using Microsoft.AspNetCore.Identity;

namespace UserService.Models
{
	public class AppUser :  IdentityUser
	{
		// Add custom properties if needed
		public string? RefreshToken { get; set; }
		public DateTime? RefreshTokenExpiryTime { get; set; }
	}

	/*
	public class UserOrdersWithProductsDto
	{
		public string UserId { get; set; }
		public string UserName { get; set; }

		public List<OrderWithProductsDto> Orders { get; set; } = new();
	}

	public class OrderWithProductsDto
	{
		public Guid OrderId { get; set; }
		public DateTime CreatedAt { get; set; }
		public decimal TotalPrice { get; set; }

		public List<ProductItemDto> Products { get; set; } = new();
	}

	public class ProductItemDto
	{
		public Guid ProductId { get; set; }

		public string Name { get; set; }

		public int Quantity { get; set; }

		public decimal Price { get; set; }
	}
	*/
}
