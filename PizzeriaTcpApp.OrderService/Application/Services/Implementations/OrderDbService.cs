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

	public Order? GetOrderById(Guid id)
	{
		using var context = new OrderDbContext();
		return context.Orders
			.Include(o => o.Items)
			.AsNoTracking()
			.FirstOrDefault(o => o.Id == id);
	}

	public bool DeleteOrder(Guid id)
	{
		using var context = new OrderDbContext();
		var order = context.Orders.Include(o => o.Items).FirstOrDefault(o => o.Id == id);
		if (order == null) return false;

		context.Orders.Remove(order);

		context.SaveChanges();
		return true;
	}

	public Order UpdateOrder(Guid id, Order updatedOrder)
	{
		using var context = new OrderDbContext();
		var existingOrder = context.Orders.Include(o => o.Items).FirstOrDefault(o => o.Id == id);
		if (existingOrder == null) return null;

		existingOrder.CustomerName = updatedOrder.CustomerName;
		existingOrder.Status = updatedOrder.Status;
		existingOrder.OrderDate = updatedOrder.OrderDate;

		var updatedItemIds = updatedOrder.Items.Select(i => i.Id).ToHashSet();

		var itemsToRemove = existingOrder.Items
			.Where(existingItem => !updatedItemIds.Contains(existingItem.Id))
			.ToList();

		if (itemsToRemove.Any())
		{
			context.OrderItems.RemoveRange(itemsToRemove);
		}

		foreach (var updatedItem in updatedOrder.Items)
		{
			var existingItem = existingOrder.Items.FirstOrDefault(e => e.Id == updatedItem.Id);

			if (existingItem != null)
			{
				existingItem.PizzaId = updatedItem.PizzaId;
				existingItem.PizzaName = updatedItem.PizzaName;
				existingItem.Quantity = updatedItem.Quantity;
				existingItem.PriceAtTimeOfOrder = updatedItem.PriceAtTimeOfOrder;
			}
			else
			{
				existingOrder.Items.Add(updatedItem);
			}
		}

		existingOrder.TotalPrice = existingOrder.Items.Sum(i => i.Quantity * i.PriceAtTimeOfOrder);

		context.SaveChanges();
		return existingOrder;
	}
}
