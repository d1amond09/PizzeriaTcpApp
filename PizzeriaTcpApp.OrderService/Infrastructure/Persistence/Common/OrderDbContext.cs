using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using PizzeriaTcpApp.OrderService.Domain.Models;

namespace PizzeriaTcpApp.OrderService.Infrastructure.Persistence.Common;

public class OrderDbContext : DbContext
{
	public DbSet<Order> Orders { get; set; }
	public DbSet<OrderItem> OrderItems { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		// Указываем использовать отдельный файл БД для этого микросервиса
		optionsBuilder.UseSqlite("Data Source=orders.db");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
	}
}