using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using UserService.Models;

namespace UserService.Data
{
	public class UserDbContext : IdentityDbContext<AppUser>
	{
		public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }
	}
}
