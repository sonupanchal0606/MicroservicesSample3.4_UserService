﻿namespace ProductService.Models
{
	public class ProductUpdateDto
	{
		public string? Name { get; set; } = string.Empty;
		public int? Quantity { get; set; }
		public decimal? Price { get; set; }
	}
}
