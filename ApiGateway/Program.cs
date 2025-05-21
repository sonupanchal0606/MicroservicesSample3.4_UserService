using ApiGateway;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddTransient<ApiKeyDelegatingHandler>();

var key = builder.Configuration["Jwt:Key"];
builder.Services.AddAuthentication("Bearer")
	.AddJwtBearer("Bearer", options =>
	{
		options.Authority = "https://localhost:5000"; // UserService URL
		options.RequireHttpsMetadata = true; // if using HTTPS
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
			ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
/*			ValidateAudience = false,
            NameClaimType = "name",
            RoleClaimType = "role" // IMPORTANT: Maps `role` in JWT to .NET roles*/
			RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", // map correct claim
   
		};
	});
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();
app.Run();
