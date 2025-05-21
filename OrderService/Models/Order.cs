namespace OrderService.Models
{
	public class Order
	{
		public Guid Id { get; set; }
		public Guid ProductId { get; set; }
		public int Quantity { get; set; }
		public Guid UserId { get; set; }   // New
		public decimal TotalPrice { get; set; }
	}


}
