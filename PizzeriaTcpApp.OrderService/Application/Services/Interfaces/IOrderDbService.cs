using PizzeriaTcpApp.OrderService.Domain.Models;

namespace PizzeriaTcpApp.OrderService.Application.Services.Interfaces;

internal interface IOrderDbService
{
	Order CreateOrder(Order newOrder);
	IEnumerable<Order> GetAllOrders();
	Order? GetOrderById(Guid id);
	Order UpdateOrder(Guid id, Order updatedOrder);
	bool DeleteOrder(Guid id);
}
