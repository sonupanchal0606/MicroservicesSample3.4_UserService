namespace OrderService.Models
{
	public class OrderRequestDto
	{
		public Guid ProductId { get; set; }
		public Guid UserId { get; set; }   // New
		public int Quantity { get; set; }

	}
}
