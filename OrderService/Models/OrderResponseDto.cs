namespace OrderService.Models
{
	public class OrderResponseDto
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; }   // New
		public int Quantity { get; set; }
		public decimal TotalPrice { get; set; }

		public UserDto User { get; set; }
		public ProductDto Product { get; set; }
	}
}
