﻿namespace ProductService.Models
{
	public class ProductCreateDto
	{
		public string Name { get; set; } = string.Empty;
		public int Quantity { get; set; }
		public decimal Price { get; set; }
	}
}
