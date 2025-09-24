using Microsoft.EntityFrameworkCore;
using PizzeriaTcpApp.OrderService.Application.Services.Interfaces;
using PizzeriaTcpApp.OrderService.Domain.Models;
using PizzeriaTcpApp.OrderService.Infrastructure.Persistence.Common;

namespace PizzeriaTcpApp.OrderService.Application.Services.Implementations;

public class OrderDbService : IOrderDbService
{
	public OrderDbService()
	{
		using var context = new OrderDbContext();
		context.Database.Migrate();
	}

	public Order CreateOrder(Order newOrder)
	{
		using var context = new OrderDbContext();
		context.Orders.Add(newOrder);
		context.SaveChanges();
		return newOrder;
	}

	public IEnumerable<Order> GetAllOrders()
	{
		using var context = new OrderDbContext();
		return [.. context.Orders
			.Include(o => o.Items)
			.AsNoTracking()];
	}
}
