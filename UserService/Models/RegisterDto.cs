namespace UserService.Models
{
	public class RegisterDto
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public string Role { get; set; } // "Admin" or "Customer"
	}
}
