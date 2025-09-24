using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PizzeriaTcpApp.PizzaService.Domain.Models;

namespace PizzeriaTcpApp.PizzaService.Infrastructure.Persistence.Common;

public class AppDbContext : DbContext
{
	public DbSet<Pizza> Pizzas { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSqlite("Data Source=pizzeria.db");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Pizza>()
			.Property(p => p.Ingredients)
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
				v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
			);
	}
}
